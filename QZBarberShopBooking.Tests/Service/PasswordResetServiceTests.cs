using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Infrastructure.Repositories;
using QZBarberShopBooking.Service.Notifications;
using QZBarberShopBooking.Service.Password;
using QZBarberShopBooking.Tests.TestSupport;
using Xunit;
using PasswordResetServiceUnderTest = QZBarberShopBooking.Service.Auth.PasswordResetService;

namespace QZBarberShopBooking.Tests.Service;

/// <summary>
/// Coverage for the reset-code hardening: a 6-digit numeric code (not a 32-byte token), a 10
/// minute expiry (not 1 hour), and a hard cap on wrong guesses that invalidates the code outright
/// — without the cap, the 1,000,000-value code space is brute-forceable well inside its lifetime.
/// Each verify attempt gets its own DbContext (via CreateSut), matching how a real HTTP request
/// always gets its own scoped DbContext with nothing pre-tracked — reusing one context across
/// several attempts would make Repository.Update's Attach() collide with the previous attempt's
/// still-tracked instance, which a real multi-request flow never does.
/// </summary>
public class PasswordResetServiceTests
{
    private static void SeedCustomer(string databaseName)
    {
        var seedContext = TestContextFactory.CreateContext(databaseName);
        seedContext.UserRoles.Add(new UserRole { Id = 1, Name = "Customer" });
        seedContext.Customers.Add(new Customer
        {
            Id = 1, Username = "cara", Email = "cara@test.local", PasswordHash = "x",
            FirstName = "Cara", LastName = "Customer", PhoneNumber = "1", RoleId = 1
        });
        seedContext.SaveChanges();
    }

    private static PasswordResetServiceUnderTest CreateSut(string databaseName, IEmailSender? emailSender = null)
    {
        var context = TestContextFactory.CreateContext(databaseName);
        IRepository<T> Repo<T>() where T : class => new Repository<T>(context);

        return new PasswordResetServiceUnderTest(
            Repo<Domain.Entities.User>(),
            new PasswordService(),
            new UnitOfWork(context, TestContextFactory.CreateCacheService()),
            emailSender ?? new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance));
    }

    private static Customer ReadCustomer(string databaseName) =>
        TestContextFactory.CreateContext(databaseName).Set<Customer>().Single(u => u.Email == "cara@test.local");

    [Fact]
    public async Task ResetPasswordAsync_GeneratesASixDigitCode_ExpiringInTenMinutes()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedCustomer(databaseName);

        await ResetAndCaptureCodeAsync(databaseName);

        var user = ReadCustomer(databaseName);
        Assert.NotNull(user.ResetPasswordTokenExpiry);
        var minutesUntilExpiry = (user.ResetPasswordTokenExpiry!.Value - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(minutesUntilExpiry, 9, 10);
        Assert.Equal(0, user.ResetPasswordAttempts);
    }

    [Fact]
    public async Task VerifyResetTokenAsync_Succeeds_ForTheCodeJustEmailed()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedCustomer(databaseName);
        var code = await ResetAndCaptureCodeAsync(databaseName);

        await CreateSut(databaseName).VerifyResetTokenAsync("cara@test.local", code, "NewP@ssw0rd1");

        var user = ReadCustomer(databaseName);
        Assert.Null(user.ResetPasswordToken);
        Assert.Null(user.ResetPasswordTokenExpiry);
        Assert.Equal(0, user.ResetPasswordAttempts);
    }

    [Fact]
    public async Task VerifyResetTokenAsync_IncrementsAttempts_ForAWrongCode()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedCustomer(databaseName);
        await ResetAndCaptureCodeAsync(databaseName);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateSut(databaseName).VerifyResetTokenAsync("cara@test.local", "000000", "NewP@ssw0rd1"));

        var user = ReadCustomer(databaseName);
        Assert.Equal(1, user.ResetPasswordAttempts);
        Assert.NotNull(user.ResetPasswordToken); // still usable — only one bad guess so far
    }

    [Fact]
    public async Task VerifyResetTokenAsync_InvalidatesTheCode_AfterFiveWrongGuesses()
    {
        var databaseName = Guid.NewGuid().ToString();
        SeedCustomer(databaseName);
        var code = await ResetAndCaptureCodeAsync(databaseName);
        var wrongCode = code == "111111" ? "222222" : "111111";

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                CreateSut(databaseName).VerifyResetTokenAsync("cara@test.local", wrongCode, "NewP@ssw0rd1"));
        }

        // The code was still correct, but the attempt budget is exhausted — even the right code
        // must now be rejected until the user requests a fresh one.
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateSut(databaseName).VerifyResetTokenAsync("cara@test.local", code, "NewP@ssw0rd1"));

        var user = ReadCustomer(databaseName);
        Assert.Null(user.ResetPasswordToken);
        Assert.Null(user.ResetPasswordTokenExpiry);
    }

    private static async Task<string> ResetAndCaptureCodeAsync(string databaseName)
    {
        string? capturedCode = null;
        var emailSender = new CapturingEmailSender(body => capturedCode = ExtractCode(body));

        await CreateSut(databaseName, emailSender).ResetPasswordAsync("cara@test.local");

        Assert.NotNull(capturedCode);
        Assert.Matches("^\\d{6}$", capturedCode!);
        return capturedCode!;
    }

    private static string ExtractCode(string emailBody) =>
        Regex.Match(emailBody, @"\d{6}").Value;

    private sealed class CapturingEmailSender(Action<string> onSend) : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            onSend(body);
            return Task.CompletedTask;
        }
    }
}
