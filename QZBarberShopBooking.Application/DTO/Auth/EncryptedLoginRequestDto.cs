namespace QZBarberShopBooking.Application.DTO.Auth
{
    /// <summary>The mobile app RSA-OAEP-SHA256 encrypts the {"email":...,"password":...} JSON with
    /// the server's public key (see ILoginPayloadDecryptor) before it ever leaves the device, so
    /// credentials never travel — or get logged — as plaintext.</summary>
    public class EncryptedLoginRequestDto
    {
        public required string Payload { get; set; }
    }
}
