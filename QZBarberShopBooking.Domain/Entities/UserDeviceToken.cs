namespace QZBarberShopBooking.Domain.Entities
{
    public class UserDeviceToken : TEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string DeviceToken { get; set; } = null!;
        public string? DeviceType { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
