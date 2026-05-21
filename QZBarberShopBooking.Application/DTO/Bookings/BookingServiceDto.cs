using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class BookingServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal Price { get; set; }
        public TimeSpan Duration { get; set; }
        // Stored/returned in UTC
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
    }
}
