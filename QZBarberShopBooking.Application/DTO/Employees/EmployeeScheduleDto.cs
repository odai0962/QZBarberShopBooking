using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Employees
{
    public class EmployeeScheduleDto
    {
        public int EmployeeId { get; set; }
        public List<EmployeeScheduleDayDto> ScheduleDays { get; set; } = new();
        public List<EmployeeTimeOffDto> TimeOffs { get; set; } = new();
    }
}
