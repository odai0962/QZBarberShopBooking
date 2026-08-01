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

            var token = GenerateResetToken();
            var hashed = TokenHashing.HashSha256(token);
            user.ResetPasswordToken = hashed;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Only the hash is ever persisted (above) — the raw token exists solely in this
            // request and must be delivered out-of-band now, or it's unrecoverable.
            await _emailSender.SendAsync(
                user.Email,
                "Reset your Q&Z Barber Shop password",
                $"Your password reset code is: {token}\nThis code expires in 1 hour. If you didn't request this, you can ignore this email.",
                cancellationToken);

            return true;
        }

        public async Task<bool> VerifyResetTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetAll()
                .FirstOrDefaultAsync(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant(), cancellationToken)
                ?? throw new NotFoundException("User", email);

            var hashed = TokenHashing.HashSha256(token);
            if (!user.ResetPasswordTokenExpiry.HasValue || user.ResetPasswordTokenExpiry.Value < DateTime.UtcNow)
                throw new UnauthorizedException("Invalid or expired reset token");

            var stored = user.ResetPasswordToken ?? string.Empty;
            var storedBytes = Base64UrlEncoder.DecodeBytes(stored);
            var hashedBytes = Base64UrlEncoder.DecodeBytes(hashed);
            if (!CryptographicOperations.FixedTimeEquals(storedBytes, hashedBytes))
                throw new UnauthorizedException("Invalid or expired reset token");

            user.PasswordHash = _passwordService.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static string GenerateResetToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncoder.Encode(bytes);
        }
    }
}
