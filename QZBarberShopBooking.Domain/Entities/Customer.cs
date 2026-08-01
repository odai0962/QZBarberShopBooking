using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{

    public class Customer : User
    {
        public DateTime? DateOfBirth { get; set; }
        public int? TotalVisits { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
