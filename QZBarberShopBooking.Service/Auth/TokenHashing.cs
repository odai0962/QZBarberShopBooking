using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace QZBarberShopBooking.Service.Auth
{
    // Shared by TokenService (refresh tokens) and PasswordResetService (reset tokens) — both
    // only ever persist this hash, never the raw token, so the hashing algorithm needs to stay
    // identical between whoever writes it and whoever verifies it later.
    internal static class TokenHashing
    {
        public static string HashSha256(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Base64UrlEncoder.Encode(hash);
        }
    }
}
