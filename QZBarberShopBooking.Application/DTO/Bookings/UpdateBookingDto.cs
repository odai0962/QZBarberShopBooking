using QZBarberShopBooking.Application.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class UpdateBookingDto
    {
        public DateTime? BookingDate { get; set; }
        public BookingStatus? Status { get; set; }
        public string? Notes { get; set; }
        public int? EmployeeId { get; set; }
    }
}
