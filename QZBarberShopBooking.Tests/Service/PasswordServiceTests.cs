using QZBarberShopBooking.Service.Password;
using Xunit;

namespace QZBarberShopBooking.Tests.Service;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void VerifyPassword_ReturnsTrue_ForTheCorrectPassword()
    {
        var hash = _sut.HashPassword("Correct-Horse-1!");

        Assert.True(_sut.VerifyPassword(hash, "Correct-Horse-1!"));
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForAnIncorrectPassword()
    {
        var hash = _sut.HashPassword("Correct-Horse-1!");

        Assert.False(_sut.VerifyPassword(hash, "wrong-password"));
    }

    [Fact]
    public void HashPassword_ProducesADifferentHash_ForTheSamePasswordEachTime()
    {
        // A fresh random salt per call means two hashes of the same password must differ.
        var first = _sut.HashPassword("same-password");
        var second = _sut.HashPassword("same-password");

        Assert.NotEqual(first, second);
        Assert.True(_sut.VerifyPassword(first, "same-password"));
        Assert.True(_sut.VerifyPassword(second, "same-password"));
    }
}
