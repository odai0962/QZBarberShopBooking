using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class Service : TEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public TimeSpan DefaultDuration { get; set; }
        public decimal BasePrice { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<EmployeeService> EmployeeServices { get; set; } = new List<EmployeeService>();
        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    }

}
