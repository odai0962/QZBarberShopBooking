using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Services
{
    public class EmployeeServiceDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal CustomPrice { get; set; }
        public TimeSpan? CustomDuration { get; set; }
        public bool IsAvailable { get; set; }
    }
}
