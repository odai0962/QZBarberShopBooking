using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Application.DTO.Notifications;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Notifications;

public class NotificationService : INotificationService, IScopedService
{
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IRepository<UserDeviceToken> _deviceTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPushNotificationSender _pushSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IRepository<Notification> notificationRepository,
        IRepository<UserDeviceToken> deviceTokenRepository,
        IUnitOfWork unitOfWork,
        IPushNotificationSender pushSender,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _deviceTokenRepository = deviceTokenRepository;
        _unitOfWork = unitOfWork;
        _pushSender = pushSender;
        _logger = logger;
    }

    public Task NotifyEmployeeBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        return PersistAndSendAsync(
            booking.EmployeeId,
            NotificationType.BookingApprovalRequested,
            "New booking request",
            $"Booking #{booking.BookingNumber} on {booking.StartTimeUtc:g} needs your confirmation.",
            booking.Id,
            cancellationToken);
    }

    public Task NotifyBookingApprovalRequestedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        return PersistAndSendAsync(
            booking.CustomerId,
            NotificationType.BookingApprovalRequested,
            "Approve your appointment",
            $"Your barber booked appointment #{booking.BookingNumber} on {booking.StartTimeUtc:g} for you. Please approve or reject it.",
            booking.Id,
            cancellationToken);
    }

    public Task NotifyBookingConfirmedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        return PersistAndSendAsync(
            booking.CustomerId,
            NotificationType.BookingConfirmed,
            "Appointment confirmed",
            $"Your appointment #{booking.BookingNumber} on {booking.StartTimeUtc:g} has been confirmed.",
            booking.Id,
            cancellationToken);
    }

    public Task NotifyBookingCancelledAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        return PersistAndSendAsync(
            booking.CustomerId,
            NotificationType.BookingCancelled,
            "Appointment cancelled",
            $"Your appointment #{booking.BookingNumber} on {booking.StartTimeUtc:g} has been cancelled.",
            booking.Id,
            cancellationToken);
    }

    public Task NotifyBookingRespondedByCustomerAsync(Booking booking, bool approved, CancellationToken cancellationToken = default)
    {
        if (booking.InitiatedByUserId is not int initiatorId)
            return Task.CompletedTask;

        return PersistAndSendAsync(
            initiatorId,
            NotificationType.BookingRespondedByCustomer,
            approved ? "Booking approved" : "Booking rejected",
            approved
                ? $"The customer approved booking #{booking.BookingNumber}."
                : $"The customer rejected booking #{booking.BookingNumber}.",
            booking.Id,
            cancellationToken);
    }

    public async Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.GetAll()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreationDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Body = n.Body,
            RelatedBookingId = n.RelatedBookingId,
            IsRead = n.IsRead,
            CreationDate = n.CreationDate
        });
    }

    public async Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetAll()
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken)
            ?? throw new NotFoundException("Notification", notificationId);

        if (notification.RecipientUserId != userId)
            throw new ForbiddenException("You cannot access this notification.");

        if (notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _notificationRepository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistAndSendAsync(
        int recipientUserId,
        NotificationType type,
        string title,
        string body,
        int? relatedBookingId,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Body = body,
            RelatedBookingId = relatedBookingId,
            CreationDate = DateTime.UtcNow
        };

        await _notificationRepository.InsertAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var deviceTokens = await _deviceTokenRepository.GetAll()
            .Where(t => t.UserId == recipientUserId && t.IsActive)
            .Select(t => t.DeviceToken)
            .ToListAsync(cancellationToken);

        if (deviceTokens.Count == 0)
            return;

        // A push delivery failure must never fail the booking operation that triggered it — the
        // Notification row above already persisted, so the in-app feed still has it either way.
        try
        {
            var data = relatedBookingId.HasValue
                ? new Dictionary<string, string> { ["bookingId"] = relatedBookingId.Value.ToString() }
                : null;

            await _pushSender.SendAsync(deviceTokens, title, body, data, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push-notify user {UserId}: {Title}", recipientUserId, title);
        }
    }
}
