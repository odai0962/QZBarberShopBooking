using QZBarberShopBooking.Application.DTO.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Employees
{
    public class CreateEmployeeDto : RegisterEmployeeDto
    {
        public decimal? HourlyRate { get; set; }
        public DateTime? HireDate { get; set; }
        public bool IsAvailableForBooking { get; set; } = true;
        public List<int> ServiceIds { get; set; } = new();
    }
}
