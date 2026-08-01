using QZBarberShopBooking.Service.DI.DIType;
using System;
using System.Security.Cryptography;
using System.Text;

namespace QZBarberShopBooking.Service.Password
{
    // Simple PBKDF2 implementation for password hashing
    public class PasswordService : IScopedService
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public string HashPassword(string password)
        {
            var salt = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);

            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            var result = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, result, SaltSize, HashSize);

            return Convert.ToBase64String(result);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var bytes = Convert.FromBase64String(hashedPassword);
            var salt = new byte[SaltSize];
            Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);

            var hash = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            var storedHash = new byte[HashSize];
            Buffer.BlockCopy(bytes, SaltSize, storedHash, 0, HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }
    }
}
