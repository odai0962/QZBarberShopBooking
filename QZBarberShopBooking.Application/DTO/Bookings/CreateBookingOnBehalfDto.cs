namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class CreateBookingOnBehalfDto
    {
        public int CustomerId { get; set; }
        // Requested start time for the booking (in UTC)
        public DateTime RequestedStartUtc { get; set; }
        // Employee (barber) this booking is assigned to
        public int EmployeeId { get; set; }
        public string? Notes { get; set; }
        public List<CreateBookingServiceDto> Services { get; set; } = new();
        public int? DiscountPercentage { get; set; }
    }
}
