using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class CreateBookingServiceDto
    {
        public int ServiceId { get; set; }
        public int EmployeeId { get; set; }
        public int TimeSlotId { get; set; }
    }
}
