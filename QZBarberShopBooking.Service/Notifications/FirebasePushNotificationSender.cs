using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Notifications;

/// <summary>Sends push notifications via Firebase Cloud Messaging, reusing the same
/// FirebaseApp/service-account credential Program.cs already sets up for social-login token
/// verification (see FirebaseSocialAuthTokenVerifier) — no separate Firebase config needed.</summary>
public class FirebasePushNotificationSender : IPushNotificationSender, IScopedService
{
    private readonly ILogger<FirebasePushNotificationSender> _logger;

    public FirebasePushNotificationSender(ILogger<FirebasePushNotificationSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(
        IEnumerable<string> deviceTokens,
        string title,
        string body,
        IDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = deviceTokens.Distinct().ToList();
        if (tokens.Count == 0)
            return;

        // Same graceful-degradation pattern as FirebaseSocialAuthTokenVerifier: without a
        // configured service-account key, push just doesn't happen instead of the caller failing.
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning(
                "Push notification not sent (Firebase:ServiceAccountKeyPath is not configured): {Title}", title);
            return;
        }

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data != null ? new Dictionary<string, string>(data) : null
        };

        try
        {
            await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, "Failed to send push notification: {Title}", title);
        }
    }
}
