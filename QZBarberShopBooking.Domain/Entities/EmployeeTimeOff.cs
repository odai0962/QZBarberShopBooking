using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Domain.Entities
{
    public class EmployeeTimeOff : TEntity
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public bool IsApproved { get; set; }


        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
    }
}
