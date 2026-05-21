using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class TimeSlot :TEntity
    {

        // availability slot stored in UTC
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
    }
}
