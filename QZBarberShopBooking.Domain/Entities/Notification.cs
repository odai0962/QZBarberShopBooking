using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Domain.Entities
{
    public class Notification : TEntity
    {
        public int RecipientUserId { get; set; }
        public User Recipient { get; set; } = null!;
        public NotificationType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public int? RelatedBookingId { get; set; }
        public Booking? RelatedBooking { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    }
}
