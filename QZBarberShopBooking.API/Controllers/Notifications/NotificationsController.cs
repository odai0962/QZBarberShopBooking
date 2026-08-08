using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QZBarberShopBooking.API.Controllers.Base;
using QZBarberShopBooking.Application.DTO.Notifications;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Helpers;
using QZBarberShopBooking.Application.Interfaces;

namespace QZBarberShopBooking.API.Controllers.Notifications;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IEnumerable<NotificationDto>>>> GetMyNotifications()
    {
        var userId = UserContext.GetUserIdOrThrow();
        var notifications = await _notificationService.GetMyNotificationsAsync(userId);
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Success(notifications, "Notifications retrieved"));
    }

    [HttpPatch("{id:int}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(int id)
    {
        var userId = UserContext.GetUserIdOrThrow();
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse.Success("Notification marked as read"));
    }
}
