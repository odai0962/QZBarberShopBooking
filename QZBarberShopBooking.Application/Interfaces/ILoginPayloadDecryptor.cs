using QZBarberShopBooking.Application.DTO.Auth;

namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Decrypts the RSA-OAEP-SHA256 encrypted login payload the mobile app sends instead of
    /// a plaintext email/password body (see <see cref="EncryptedLoginRequestDto"/>). The server holds
    /// the RSA private key (LoginEncryption:PrivateKey); the client only ever has the public key.</summary>
    public interface ILoginPayloadDecryptor
    {
        /// <summary>Decrypts <paramref name="base64Payload"/> into a <see cref="LoginDto"/>.
        /// Throws <see cref="Exceptions.ValidationException"/> for any malformed/undecryptable
        /// payload — never the underlying cryptographic exception, which would leak details about
        /// why decryption failed.</summary>
        LoginDto Decrypt(string base64Payload);
    }
}
