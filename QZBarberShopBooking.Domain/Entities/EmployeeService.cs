using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class EmployeeService : TEntity
    {
        public decimal CustomPrice { get; set; }
        public TimeSpan? CustomDuration { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public int ServiceId { get; set; }
        public Service Service { get; set; }
    }
}
