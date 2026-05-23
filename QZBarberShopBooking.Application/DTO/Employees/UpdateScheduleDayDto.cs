using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Employees
{
    public class UpdateScheduleDayDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsWorkingDay { get; set; }
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
    }
}
