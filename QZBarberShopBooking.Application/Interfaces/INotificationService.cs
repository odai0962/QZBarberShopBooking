using QZBarberShopBooking.Domain.Entities;

namespace QZBarberShopBooking.Application.Interfaces;

public interface INotificationService
{
    Task NotifyEmployeeBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default);
}
