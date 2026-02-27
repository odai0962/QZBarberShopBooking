using QZBarberShopBooking.Application.DTO.Services;
using QZBarberShopBooking.Application.DTO.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Employees
{
    public class EmployeeDto : UserDto
    {
        public string? Specialization { get; set; }
        public string? Bio { get; set; }
        public decimal? HourlyRate { get; set; }
        public DateTime? HireDate { get; set; }
        public bool IsAvailableForBooking { get; set; }
        public decimal Rating { get; set; }
        public int TotalBookings { get; set; }
        public List<ServiceDto> Services { get; set; } = new();
    }

}
