using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Notifications;

/// <summary>
/// Placeholder for Firebase / SignalR push. Persist booking state; push can be wired here later.
/// </summary>
public class NotificationService : INotificationService, IScopedService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyEmployeeBookingCreatedAsync(Domain.Entities.Booking booking, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Booking notification for employee {EmployeeId}: #{BookingNumber} at {StartTimeUtc:u}",
            booking.EmployeeId,
            booking.BookingNumber,
            booking.StartTimeUtc);

        return Task.CompletedTask;
    }
}
