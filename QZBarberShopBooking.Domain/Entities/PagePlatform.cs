using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Domain.Entities
{
    public class PagePlatform : TEntity
    {
        public int PageId { get; set; }
        public Page Page { get; set; } = null!;
        public Platform Platform { get; set; }
    }
}
