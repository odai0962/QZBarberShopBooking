using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace QZBarberShopBooking.Application.Helpers
{
    public static class JWTHelper
    {
        public static string GenerateJwtToken(int userId, int roleId, string secretKey, int? expiredInSecond = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new("userId", userId.ToString()),
                new("roleId", roleId.ToString()),
                new(ClaimTypes.Role, roleId == 1 ? "Admin":"Client"),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 24)
            {
                throw new ArgumentException("JWT secret key not available.");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiredInSecond == null ? DateTime.UtcNow.AddMinutes(30) : DateTime.UtcNow.AddSeconds(expiredInSecond.Value),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
                Issuer = "VMS_API",
                Audience = "VMS_WEB",
                IssuedAt = DateTime.UtcNow,
                TokenType = "JWT",
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static ClaimsPrincipal ValidateTokenWithLifeTime(string token, string secretKey, string? validIssuer = null, string? validAudience = null, TimeSpan? clockSkew = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 24)
            {
                throw new ArgumentException("JWT secret key not available.");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = validIssuer != null,
                ValidIssuer = validIssuer,
                ValidateAudience = validAudience != null,
                ValidAudience = validAudience,
                ValidateLifetime = true,
                ClockSkew = clockSkew ?? TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }

        public static ClaimsPrincipal ValidateTokenWithoutLifetime(string token, string secretKey, string? validIssuer = null, string? validAudience = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 24)
            {
                throw new ArgumentException("JWT secret key not available.");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var principalResult = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = validIssuer != null,
                ValidIssuer = validIssuer,
                ValidateAudience = validAudience != null,
                ValidAudience = validAudience,
                // For refresh token, no need to validate life time
                ValidateLifetime = false,
            }, out SecurityToken validatedToken);

            return principalResult;
        }

        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var randomText = RandomNumberGenerator.Create();
            randomText.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public static ClaimsPrincipal GetPrincipalFromExpiredToken(string token, string secretKey)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

    }
}

