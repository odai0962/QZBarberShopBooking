using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Customers
{
    public class CustomerStatsDto
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageSpending { get; set; }
        public int DaysSinceLastVisit { get; set; }
    }
}
