using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Service.Bookings;

/// <summary>Auto-cancels Confirmed bookings nobody checked in for within the configured grace
/// period ("no-show"). There is no scheduler (Hangfire/Quartz) in this project, so this is a
/// plain hosted BackgroundService — registered explicitly via AddHostedService in Program.cs, not
/// via the IScopedService auto-registration marker, since that would give it the wrong (scoped)
/// lifetime. It resolves a fresh IServiceScope per tick because IRepository/IUnitOfWork are
/// scoped while this service itself is a singleton for the app's lifetime.</summary>
public class NoShowCancellationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NoShowCancellationService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly int _thresholdMinutes;

    public NoShowCancellationService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<NoShowCancellationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var section = configuration.GetSection("NoShowCancellation");
        _pollInterval = TimeSpan.FromMinutes(section.GetValue("PollIntervalMinutes", 5));
        _thresholdMinutes = section.GetValue("ThresholdMinutes", 20);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollInterval);

        do
        {
            try
            {
                await CancelNoShowBookingsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad sweep must not kill the timer loop for the rest of the app's lifetime.
                _logger.LogError(ex, "No-show cancellation sweep failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task CancelNoShowBookingsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IRepository<Domain.Entities.Booking>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var cutoff = DateTime.UtcNow.AddMinutes(-_thresholdMinutes);

        var overdueBookings = await bookingRepository.GetAll()
            .Where(b => b.Status == BookingStatus.Confirmed && !b.IsDeleted && b.StartTimeUtc <= cutoff)
            .ToListAsync(cancellationToken);

        if (overdueBookings.Count == 0)
            return;

        foreach (var booking in overdueBookings)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = BookingCancellationReason.NoShow;
            booking.ModificationDate = DateTime.UtcNow;

            await bookingRepository.UpdateAsync(booking, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var booking in overdueBookings)
        {
            await notificationService.NotifyBookingCancelledAsync(booking, cancellationToken);
        }

        _logger.LogInformation("Auto-cancelled {Count} no-show booking(s)", overdueBookings.Count);
    }
}
