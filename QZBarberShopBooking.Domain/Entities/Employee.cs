using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class Employee : User
    {
        public string? Specialization { get; set; }
        public string? Bio { get; set; }
        public decimal? HourlyRate { get; set; }
        public DateTime? HireDate { get; set; }
        public bool? IsAvailableForBooking { get; set; }
        public ICollection<EmployeeService> Services { get; set; } = new List<EmployeeService>();
        public ICollection<EmployeeSchedule> Schedules { get; set; } = new List<EmployeeSchedule>();
        public ICollection<EmployeeTimeOff> TimeOffs { get; set; } = new List<EmployeeTimeOff>();
        public ICollection<BookingServiceLine> BookingServices { get; set; } = new List<BookingServiceLine>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}

