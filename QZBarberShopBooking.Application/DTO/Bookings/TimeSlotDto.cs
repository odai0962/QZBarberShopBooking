using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class TimeSlotDto
    {
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public bool IsAvailable { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
    }
}
