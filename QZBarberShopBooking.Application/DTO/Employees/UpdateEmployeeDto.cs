using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Employees
{
    public class UpdateEmployeeDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Specialization { get; set; }
        public string? Bio { get; set; }
        public decimal? HourlyRate { get; set; }
        public bool? IsAvailableForBooking { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
