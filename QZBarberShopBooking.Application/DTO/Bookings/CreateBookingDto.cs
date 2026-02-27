using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class CreateBookingDto
    {
        public DateTime BookingDate { get; set; }
        public string? Notes { get; set; }
        public List<CreateBookingServiceDto> Services { get; set; } = new();
        public int? DiscountPercentage { get; set; }
    }
}
