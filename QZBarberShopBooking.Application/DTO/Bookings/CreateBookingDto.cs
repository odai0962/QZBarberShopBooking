using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class CreateBookingDto
    {
        // Requested start time for the booking (in UTC)
        public DateTime RequestedStartUtc { get; set; }
        // Customer must select the employee (barber) for this booking
        public int EmployeeId { get; set; }
        public string? Notes { get; set; }
        public List<CreateBookingServiceDto> Services { get; set; } = new();
        public int? DiscountPercentage { get; set; }
    }
}
