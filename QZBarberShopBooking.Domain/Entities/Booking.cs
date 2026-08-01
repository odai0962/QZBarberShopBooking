using QZBarberShopBooking.Domain.Common;
using QZBarberShopBooking.Domain.Enums;

namespace QZBarberShopBooking.Domain.Entities
{
    public class Booking : TEntity, IAuditable, IDeletable
    {
        public string BookingNumber { get; set; } = Guid.NewGuid().ToString()[..8].ToUpper();
        public DateTime BookingDate { get; set; }
        // overall reserved interval for the booking (stored in UTC)
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public BookingStatus Status { get; set; }
        public string? Notes { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? ModificationDate { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        // Booking must be assigned to a specific employee (barber) chosen by the customer
        public int EmployeeId { get; set; }
        public Employee AssignedEmployee { get; set; } = null!;
        public ICollection<BookingServiceLine> Services { get; set; } = new List<BookingServiceLine>();
    }
}
