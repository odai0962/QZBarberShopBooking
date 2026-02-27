using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Auth
{
    public class RegisterEmployeeDto : RegisterDto
    {
        public string? Specialization { get; set; }
        public string? Bio { get; set; }
        public decimal? HourlyRate { get; set; }
        public DateTime? HireDate { get; set; }
    }
}
