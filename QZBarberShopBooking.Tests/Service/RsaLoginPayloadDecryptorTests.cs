using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Service.Auth;
using Xunit;

namespace QZBarberShopBooking.Tests.Service;

public class RsaLoginPayloadDecryptorTests
{
    private static (RsaLoginPayloadDecryptor Sut, RSA Rsa) CreateSut()
    {
        var rsa = RSA.Create(2048);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "LoginEncryption:PrivateKey", rsa.ExportPkcs8PrivateKeyPem() }
            })
            .Build();

        return (new RsaLoginPayloadDecryptor(configuration, NullLogger<RsaLoginPayloadDecryptor>.Instance), rsa);
    }

    private static string EncryptPayload(RSA rsa, string email, string password)
    {
        var json = JsonSerializer.Serialize(new { email, password });
        var cipherBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(json), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(cipherBytes);
    }

    [Fact]
    public void Decrypt_ReturnsTheOriginalCredentials_ForAValidPayload()
    {
        var (sut, rsa) = CreateSut();
        var payload = EncryptPayload(rsa, "user@test.local", "s3cret-password");

        var result = sut.Decrypt(payload);

        Assert.Equal("user@test.local", result.Email);
        Assert.Equal("s3cret-password", result.Password);
    }

    [Fact]
    public void Decrypt_Throws_ForPayloadThatIsNotValidBase64()
    {
        var (sut, _) = CreateSut();

        Assert.Throws<ValidationException>(() => sut.Decrypt("not-base64!!"));
    }

    [Fact]
    public void Decrypt_Throws_ForCiphertextEncryptedWithADifferentKey()
    {
        var (sut, _) = CreateSut();
        using var otherKey = RSA.Create(2048);
        var payload = EncryptPayload(otherKey, "user@test.local", "s3cret-password");

        Assert.Throws<ValidationException>(() => sut.Decrypt(payload));
    }

    [Fact]
    public void Decrypt_Throws_WhenTheConfiguredPrivateKeyIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new RsaLoginPayloadDecryptor(configuration, NullLogger<RsaLoginPayloadDecryptor>.Instance);

        Assert.Throws<InvalidOperationException>(() => sut.Decrypt("anything"));
    }
}
