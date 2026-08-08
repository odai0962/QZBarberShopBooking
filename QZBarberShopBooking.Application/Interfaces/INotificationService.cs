using QZBarberShopBooking.Application.DTO.Notifications;
using QZBarberShopBooking.Domain.Entities;

namespace QZBarberShopBooking.Application.Interfaces;

public interface INotificationService
{
    Task NotifyEmployeeBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default);

    // Sent to the customer when an Employee/Admin books an appointment on their behalf and it
    // needs their approve/reject response.
    Task NotifyBookingApprovalRequestedAsync(Booking booking, CancellationToken cancellationToken = default);

    // Sent to the customer once their booking is confirmed by the barber.
    Task NotifyBookingConfirmedAsync(Booking booking, CancellationToken cancellationToken = default);

    // Sent to the customer when their booking is cancelled (by staff or by the no-show sweep).
    Task NotifyBookingCancelledAsync(Booking booking, CancellationToken cancellationToken = default);

    // Sent to whoever initiated an employee-on-behalf booking once the customer approves/rejects it.
    Task NotifyBookingRespondedByCustomerAsync(Booking booking, bool approved, CancellationToken cancellationToken = default);

    Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(int userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
}
