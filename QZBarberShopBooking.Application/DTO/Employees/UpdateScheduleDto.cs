using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Employees
{
    public class UpdateScheduleDto
    {
        public List<UpdateScheduleDayDto> ScheduleDays { get; set; } = new();
    }

}
