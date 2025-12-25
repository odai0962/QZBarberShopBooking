using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class TimeSlot :TEntity
    {

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int? BookingServiceId { get; set; } 
        public BookingService? BookingService { get; set; } 
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
    }
}
