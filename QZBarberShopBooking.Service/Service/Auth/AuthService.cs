using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Service.Auth
{
    public class AuthService : IAuthService, IScopedService
    {
        private readonly IRepository<QZBarberShopBooking.Domain.Entities.User> _userRepository;
        private readonly IRepository<UserRole> _roleRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly PasswordService _passwordService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IRepository<QZBarberShopBooking.Domain.Entities.User> userRepository,
            IRepository<UserRole> roleRepository,
            IRepository<Customer> customerRepository,
            IRepository<Employee> employeeRepository,
            PasswordService passwordService,
            IConfiguration configuration,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _customerRepository = customerRepository;
            _employeeRepository = employeeRepository;
            _passwordService = passwordService;
            _configuration = configuration;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        #region Auth

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetAll()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower(), cancellationToken);

            if (user == null || !_passwordService.VerifyPassword(user.PasswordHash, loginDto.Password))
                throw new UnauthorizedException("Invalid email or password");

            if (!user.IsActive)
                throw new UnauthorizedException("Account is deactivated");

            var tokens = GenerateTokens(user);
            await SetRefreshTokenAsync(user, tokens, cancellationToken);
            return tokens;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
        {
            if (await _userRepository.AnyAsync(u => u.Email.ToLower() == registerDto.Email.ToLower()))
                throw new ValidationException(new Dictionary<string, string[]> { { "Email", new[] { "Email already registered" } } });

            if (await _userRepository.AnyAsync(u => u.Username.ToLower() == registerDto.Username.ToLower()))
                throw new ValidationException(new Dictionary<string, string[]> { { "Username", new[] { "Username already taken" } } });

            var role = await _roleRepository.GetAll()
                .FirstOrDefaultAsync(r => r.Name == "Customer", cancellationToken)
                ?? throw new NotFoundException("Role", "Customer");

            var customer = _mapper.Map<Customer>(registerDto);
            customer.PasswordHash = _passwordService.HashPassword(registerDto.Password);
            customer.RoleId = role.Id;
            customer.IsActive = true;
            customer.CreationDate = DateTime.UtcNow;

            await _customerRepository.InsertAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var tokens = GenerateTokens(customer);

            await SetRefreshTokenAsync(customer, tokens, cancellationToken);
            return tokens;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default)
        {
            var principal = JWTHelper.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken, _configuration["JwtSettings:SecretKey"]);

            var userIdClaim = principal.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Invalid access token");

            var user = await _userRepository.GetAll()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && u.RefreshToken == refreshTokenDto.RefreshToken, cancellationToken);

            if (user == null)
                throw new UnauthorizedException("Invalid refresh token");

            if (!user.RefreshTokenExpiryTime.HasValue || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedException("Refresh token expired");

            var tokens = GenerateTokens(user);
            await SetRefreshTokenAsync(user, tokens, cancellationToken);
            return tokens;
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

        #endregion

        #region Tokens

        private AuthResponseDto GenerateTokens(QZBarberShopBooking.Domain.Entities.User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expireMinutes = int.Parse(jwtSettings["ExpireMinutes"] ?? "60");

            var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(JwtRegisteredClaimNames.GivenName, user.FirstName),
        new(JwtRegisteredClaimNames.FamilyName, user.LastName),
        new("userId", user.Id.ToString()),
        new("roleId", user.RoleId.ToString()),
        new(ClaimTypes.Role, user.Role?.Name ?? "User"),
        new("fullName", $"{user.FirstName} {user.LastName}"),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            if (user is Employee employee)
            {
                claims.Add(new("userType", "Employee"));
                claims.Add(new("isAvailable", (employee.IsAvailableForBooking ?? true).ToString()));
            }
            else if (user is Customer)
            {
                claims.Add(new("userType", "Customer"));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            var refreshToken = GenerateRefreshToken();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpireAt = token.ValidTo,
                UserId = user.Id,
                Role = user.Role?.Name ?? "User",
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}"
            };
        }
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateResetToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            // use Base64Url to be URL-safe
            return Base64UrlEncoder.Encode(bytes);
        }

        private static string HashTokenSha256(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Base64UrlEncoder.Encode(hash);
        }

        private async Task SetRefreshTokenAsync(QZBarberShopBooking.Domain.Entities.User user, AuthResponseDto tokens, CancellationToken cancellationToken)
        {
            user.RefreshToken = tokens.RefreshToken;
            var days = int.TryParse(_configuration["JwtSettings:RefreshTokenDays"], out var d) ? d : 7;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(days);

            switch (user)
            {
                case Customer customer:
                    await _customerRepository.UpdateAsync(customer, cancellationToken);
                    break;
                case Employee employee:
                    await _employeeRepository.UpdateAsync(employee, cancellationToken);
                    break;
                default:
                    await _userRepository.UpdateAsync(user, cancellationToken);
                    break;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Register an employee account
        public async Task<AuthResponseDto> RegisterEmployeeAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
        {
            // Prefer normalized columns if available (NormalizedEmail / NormalizedUserName)
            var normalizedEmail = registerDto.Email.ToUpperInvariant();
            var normalizedUsername = registerDto.Username.ToUpperInvariant();

            bool emailExists = false;
            bool usernameExists = false;

            // helper to build expression dynamically: e => e.Property == value
            Expression<Func<QZBarberShopBooking.Domain.Entities.User, bool>> BuildEquals(string propertyName, string value)
            {
                var param = Expression.Parameter(typeof(QZBarberShopBooking.Domain.Entities.User), "e");
                var prop = Expression.PropertyOrField(param, propertyName);
                var constant = Expression.Constant(value);
                var body = Expression.Equal(prop, constant);
                return Expression.Lambda<Func<QZBarberShopBooking.Domain.Entities.User, bool>>(body, param);
            }

            var userType = typeof(QZBarberShopBooking.Domain.Entities.User);
            if (userType.GetProperty("NormalizedEmail") != null)
            {
                var expr = BuildEquals("NormalizedEmail", normalizedEmail);
                emailExists = await _userRepository.AnyAsync(expr);
            }
            else
            {
                emailExists = await _userRepository.AnyAsync(u => u.Email.ToLower() == registerDto.Email.ToLower());
            }

            if (userType.GetProperty("NormalizedUserName") != null)
            {
                var expr = BuildEquals("NormalizedUserName", normalizedUsername);
                usernameExists = await _userRepository.AnyAsync(expr);
            }
            else
            {
                usernameExists = await _userRepository.AnyAsync(u => u.Username.ToLower() == registerDto.Username.ToLower());
            }

            if (emailExists)
                throw new ValidationException(new Dictionary<string, string[]> { { "Email", new[] { "Email already registered" } } });
            if (usernameExists)
                throw new ValidationException(new Dictionary<string, string[]> { { "Username", new[] { "Username already taken" } } });

            var role = await _roleRepository.GetAll()
                .FirstOrDefaultAsync(r => r.Name == "Employee", cancellationToken)
                ?? throw new NotFoundException("Role", "Employee");

            var employee = _mapper.Map<Employee>(registerDto);
            employee.PasswordHash = _passwordService.HashPassword(registerDto.Password);
            employee.RoleId = role.Id;
            employee.IsActive = true;
            employee.CreationDate = DateTime.UtcNow;
            employee.HireDate = DateTime.UtcNow;

            await _employeeRepository.InsertAsync(employee, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var tokens = GenerateTokens(employee);
            await SetRefreshTokenAsync(employee, tokens, cancellationToken);

            return tokens;
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
            var user = await _userRepository.GetAll()
                .FirstOrDefaultAsync(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant(), cancellationToken)
                ?? throw new NotFoundException("User", email);

            var token = GenerateResetToken();
            var hashed = HashTokenSha256(token);
            // store hashed token in URL-safe Base64
            user.ResetPasswordToken = hashed;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // TODO: send raw token to user's email via Email service
            return true;
        }

        public async Task<bool> VerifyResetTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetAll()
                .FirstOrDefaultAsync(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant(), cancellationToken)
                ?? throw new NotFoundException("User", email);

            var hashed = HashTokenSha256(token);
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

        #endregion
    }
}
