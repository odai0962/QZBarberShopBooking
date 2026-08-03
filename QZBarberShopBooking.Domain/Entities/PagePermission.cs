namespace QZBarberShopBooking.Domain.Entities
{
    public class PagePermission : TEntity
    {
        public int PageId { get; set; }
        public Page Page { get; set; } = null!;
        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}
