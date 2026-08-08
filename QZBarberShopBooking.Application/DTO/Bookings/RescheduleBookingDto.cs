namespace QZBarberShopBooking.Application.DTO.Bookings
{
    public class RescheduleBookingDto
    {
        // Requested new start time for the booking (in UTC)
        public DateTime RequestedStartUtc { get; set; }
    }
}
