using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Application.DTO.Notifications
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public required string Title { get; set; }
        public required string Body { get; set; }
        public int? RelatedBookingId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
