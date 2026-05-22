using QZBarberShopBooking.Application.DTO.Customers;
using QZBarberShopBooking.Application.DTO.Employees;
using QZBarberShopBooking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class BookingDto
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; }
        public DateTime BookingDate { get; set; }
        // Summary times (UTC)
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public BookingStatus Status { get; set; }
        public string? Notes { get; set; }
        public decimal SubTotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public CustomerDto Customer { get; set; }
        public EmployeeDto Employee { get; set; }
        public List<BookingServiceDto> Services { get; set; } = new();
    }
}
