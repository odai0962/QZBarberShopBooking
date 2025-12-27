using QZBarberShopBooking.Application.DTO.Employees;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Services
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public TimeSpan DefaultDuration { get; set; }
        public decimal BasePrice { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<EmployeeDto> AvailableEmployees { get; set; } = new();
    }
}
