namespace QZBarberShopBooking.Application.Interfaces;

// Separated from INotificationService so the actual push transport (Firebase) is independently
// mockable in tests without needing a real service-account key configured.
public interface IPushNotificationSender
{
    Task SendAsync(
        IEnumerable<string> deviceTokens,
        string title,
        string body,
        IDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
