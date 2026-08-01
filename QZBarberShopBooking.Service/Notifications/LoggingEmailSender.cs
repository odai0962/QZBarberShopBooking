using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Notifications
{
    // Logs instead of actually delivering email. This makes the password-reset flow's behavior
    // observable and testable instead of silently discarding the token, without inventing a real
    // SMTP/SendGrid/SES integration this codebase has no credentials or provider choice for yet.
    // Swap for a real IEmailSender implementation, registered the same way, once one is chosen.
    public class LoggingEmailSender : IEmailSender, IScopedService
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Email queued for {ToEmail} — {Subject}\n{Body}", toEmail, subject, body);
            return Task.CompletedTask;
        }
    }
}
