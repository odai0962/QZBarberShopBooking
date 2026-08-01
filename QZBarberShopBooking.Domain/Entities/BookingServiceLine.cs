using QZBarberShopBooking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class BookingServiceLine : TEntity, IAuditable, IDeletable
    {
        public int ServiceId { get; set; }
        public Service Service { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public decimal Price { get; set; }
        // Actual reserved interval for this service (stored in UTC)
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }

        public bool IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? ModificationDate { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

    }
}
