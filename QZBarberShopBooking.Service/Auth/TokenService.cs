using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Auth
{
    public class TokenService : ITokenService, IScopedService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public TokenService(
            IRepository<User> userRepository,
            IRepository<Customer> customerRepository,
            IRepository<Employee> employeeRepository,
            IConfiguration configuration,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _employeeRepository = employeeRepository;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponseDto> IssueTokensAsync(User user, CancellationToken cancellationToken = default)
        {
            var tokens = GenerateTokens(user);

            user.RefreshToken = TokenHashing.HashSha256(tokens.RefreshToken);
            var days = int.TryParse(_configuration["JwtSettings:RefreshTokenExpireDays"], out var d)
                ? d
                : int.TryParse(_configuration["JwtSettings:RefreshTokenDays"], out var legacy) ? legacy : 7;
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

            return tokens;
        }

        private AuthResponseDto GenerateTokens(User user)
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

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = GenerateRefreshToken(),
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
    }
}
