using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;
using QZBarberShopBooking.Service.Password;

namespace QZBarberShopBooking.Service.Auth
{
    public class AuthenticationService : IAuthenticationService, IScopedService
    {
        private readonly IRepository<User> _userRepository;
        private readonly PasswordService _passwordService;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public AuthenticationService(
            IRepository<User> userRepository,
            PasswordService passwordService,
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetAll()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower(), cancellationToken);

            if (user == null || !_passwordService.VerifyPassword(user.PasswordHash, loginDto.Password))
                throw new UnauthorizedException("Invalid email or password");

            if (!user.IsActive)
                throw new UnauthorizedException("Account is deactivated");

            return await _tokenService.IssueTokensAsync(user, cancellationToken);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT Secret Key is not configured");

            var principal = JWTHelper.GetPrincipalFromExpiredToken(
                refreshTokenDto.AccessToken,
                secretKey,
                jwtSettings["Issuer"],
                jwtSettings["Audience"]);

            var userIdClaim = principal.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Invalid access token");

            var user = await _userRepository.GetAll()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null || !RefreshTokensMatch(user.RefreshToken, refreshTokenDto.RefreshToken))
                throw new UnauthorizedException("Invalid refresh token");

            if (!user.RefreshTokenExpiryTime.HasValue || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedException("Refresh token expired");

            return await _tokenService.IssueTokensAsync(user, cancellationToken);
        }

        public async Task<bool> LogoutAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static bool RefreshTokensMatch(string? storedToken, string providedToken)
        {
            if (string.IsNullOrWhiteSpace(storedToken))
                return false;

            if (storedToken == providedToken)
                return true;

            return TokenHashing.HashSha256(providedToken) == storedToken;
        }
    }
}
