using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Auth
{
    /// <summary>RSA-OAEP-SHA256 counterpart to the mobile app's public-key encryption of the login
    /// payload. The private key (PEM, PKCS#8 or PKCS#1) comes from configuration — user-secrets in
    /// Development, the LoginEncryption__PrivateKey environment variable in Production — the same
    /// pattern used for JwtSettings:SecretKey.</summary>
    public class RsaLoginPayloadDecryptor : ILoginPayloadDecryptor, IScopedService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IConfiguration _configuration;
        private readonly ILogger<RsaLoginPayloadDecryptor> _logger;

        public RsaLoginPayloadDecryptor(IConfiguration configuration, ILogger<RsaLoginPayloadDecryptor> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public LoginDto Decrypt(string base64Payload)
        {
            var privateKeyPem = _configuration["LoginEncryption:PrivateKey"];
            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new InvalidOperationException(
                    "Login encryption private key is not configured. For Development, run: dotnet user-secrets set \"LoginEncryption:PrivateKey\" \"<PEM>\" --project QZBarberShopBooking.API. " +
                    "For Production, set environment variable LoginEncryption__PrivateKey.");

            byte[] cipherBytes;
            try
            {
                cipherBytes = Convert.FromBase64String(base64Payload);
            }
            catch (FormatException)
            {
                throw PayloadValidationError("Payload is not valid base64");
            }

            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            byte[] jsonBytes;
            try
            {
                jsonBytes = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt login payload");
                throw PayloadValidationError("Payload could not be decrypted");
            }

            LoginDto? loginDto;
            try
            {
                loginDto = JsonSerializer.Deserialize<LoginDto>(Encoding.UTF8.GetString(jsonBytes), JsonOptions);
            }
            catch (JsonException)
            {
                loginDto = null;
            }

            return loginDto ?? throw PayloadValidationError("Decrypted payload is not a valid login request");
        }

        private static ValidationException PayloadValidationError(string message) =>
            new(new Dictionary<string, string[]> { { "payload", new[] { message } } });
    }
}
