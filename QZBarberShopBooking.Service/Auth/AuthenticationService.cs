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
        private readonly IRepository<UserRole> _roleRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly PasswordService _passwordService;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly ISocialAuthTokenVerifier _socialAuthTokenVerifier;

        public AuthenticationService(
            IRepository<User> userRepository,
            IRepository<UserRole> roleRepository,
            IRepository<Customer> customerRepository,
            PasswordService passwordService,
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            ISocialAuthTokenVerifier socialAuthTokenVerifier)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _customerRepository = customerRepository;
            _passwordService = passwordService;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _socialAuthTokenVerifier = socialAuthTokenVerifier;
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

        public async Task<AuthResponseDto> SocialLoginAsync(SocialLoginDto socialLoginDto, CancellationToken cancellationToken = default)
        {
            var principal = await _socialAuthTokenVerifier.VerifyAsync(socialLoginDto.IdToken, cancellationToken);

            if (!principal.EmailVerified)
                throw new UnauthorizedException("Your account's email address is not verified with the sign-in provider");

            var user = await _userRepository.GetAll()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == principal.Email.ToLower(), cancellationToken);

            if (user == null)
            {
                var role = await _roleRepository.GetAll()
                    .FirstOrDefaultAsync(r => r.Name == "Customer", cancellationToken)
                    ?? throw new NotFoundException("Role", "Customer");

                var customer = new Customer
                {
                    Username = await GenerateUniqueUsernameAsync(principal.Email, cancellationToken),
                    Email = principal.Email,
                    // Social sign-in never uses a password — hash a random value so the account
                    // has no usable password (the user can set one later via change-password).
                    PasswordHash = _passwordService.HashPassword(Guid.NewGuid().ToString("N")),
                    PhoneNumber = string.Empty,
                    FirstName = ExtractFirstName(principal.DisplayName, principal.Email),
                    LastName = ExtractLastName(principal.DisplayName),
                    RoleId = role.Id,
                    IsActive = true,
                    CreationDate = DateTime.UtcNow,
                    Role = role
                };

                await _customerRepository.InsertAsync(customer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                user = customer;
            }
            else if (!user.IsActive)
            {
                throw new UnauthorizedException("Account is deactivated");
            }

            return await _tokenService.IssueTokensAsync(user, cancellationToken);
        }

        private async Task<string> GenerateUniqueUsernameAsync(string email, CancellationToken cancellationToken)
        {
            var baseUsername = email.Split('@')[0].ToLowerInvariant();
            var candidate = baseUsername;
            var suffix = 1;
            while (await _userRepository.AnyAsync(u => u.Username == candidate, cancellationToken))
            {
                candidate = $"{baseUsername}{suffix++}";
            }
            return candidate;
        }

        private static string ExtractFirstName(string? displayName, string email)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return email.Split('@')[0];
            return displayName.Trim().Split(' ', 2)[0];
        }

        private static string ExtractLastName(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;
            var parts = displayName.Trim().Split(' ', 2);
            return parts.Length > 1 ? parts[1] : string.Empty;
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
