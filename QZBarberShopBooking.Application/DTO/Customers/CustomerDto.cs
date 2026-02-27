using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Application.DTO.Bookings;
using QZBarberShopBooking.Application.DTO.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Customers
{
    public class CustomerDto : UserDto
    {
        public DateTime? DateOfBirth { get; set; }
        public int TotalVisits { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public decimal TotalSpent { get; set; }
        public List<BookingDto> RecentBookings { get; set; } = new();
    }
}
