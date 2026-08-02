using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;
using QZBarberShopBooking.Service.Password;

namespace QZBarberShopBooking.Service.Auth
{
    public class PasswordResetService : IPasswordResetService, IScopedService
    {
        // A 6-digit code only has 1,000,000 possible values, so a short expiry and a hard cap on
        // guesses (after which the code is invalidated outright) are load-bearing, not defense in
        // depth — without them the code space is brute-forceable well within its lifetime.
        private const int ResetCodeExpiryMinutes = 10;
        private const int MaxResetAttempts = 5;

        private readonly IRepository<User> _userRepository;
        private readonly PasswordService _passwordService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;

        public PasswordResetService(
            IRepository<User> userRepository,
            PasswordService passwordService,
            IUnitOfWork unitOfWork,
            IEmailSender emailSender)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId) ?? throw new NotFoundException("User", userId);

            if (!_passwordService.VerifyPassword(user.PasswordHash, oldPassword))
                throw new UnauthorizedException("Invalid current password");

            user.PasswordHash = _passwordService.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetAll().FirstOrDefaultAsync(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant(), cancellationToken);
            if (user is null)
                return true;

            var code = GenerateResetCode();
            user.ResetPasswordToken = TokenHashing.HashSha256(code);
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(ResetCodeExpiryMinutes);
            user.ResetPasswordAttempts = 0;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Only the hash is ever persisted (above) — the raw code exists solely in this
            // request and must be delivered out-of-band now, or it's unrecoverable.
            await _emailSender.SendAsync(
                user.Email,
                "Reset your Q&Z Barber Shop password",
                $"Your password reset code is: {code}\nThis code expires in {ResetCodeExpiryMinutes} minutes. If you didn't request this, you can ignore this email.",
                cancellationToken);

            return true;
        }

        public async Task<bool> VerifyResetTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetAll()
                .FirstOrDefaultAsync(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant(), cancellationToken)
                ?? throw new NotFoundException("User", email);

            if (!user.ResetPasswordTokenExpiry.HasValue || user.ResetPasswordTokenExpiry.Value < DateTime.UtcNow)
                throw new UnauthorizedException("Invalid or expired reset code");

            if (user.ResetPasswordAttempts >= MaxResetAttempts)
            {
                await InvalidateResetCodeAsync(user, cancellationToken);
                throw new UnauthorizedException("Too many incorrect attempts. Request a new reset code.");
            }

            var hashed = TokenHashing.HashSha256(token);
            var stored = user.ResetPasswordToken ?? string.Empty;
            var storedBytes = Base64UrlEncoder.DecodeBytes(stored);
            var hashedBytes = Base64UrlEncoder.DecodeBytes(hashed);
            if (!CryptographicOperations.FixedTimeEquals(storedBytes, hashedBytes))
            {
                user.ResetPasswordAttempts++;
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                throw new UnauthorizedException("Invalid or expired reset code");
            }

            user.PasswordHash = _passwordService.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;
            user.ResetPasswordAttempts = 0;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task InvalidateResetCodeAsync(User user, CancellationToken cancellationToken)
        {
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;
            user.ResetPasswordAttempts = 0;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static string GenerateResetCode() =>
            RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }
}
