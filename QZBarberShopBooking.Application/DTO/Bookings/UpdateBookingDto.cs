namespace QZBarberShopBooking.Application.DTO.Bookings
{
    // Deliberately narrow: date/time changes go through /reschedule, employee changes go
    // through /transfer, and status changes go through the dedicated status-transition
    // endpoints. This closes an old gap where this DTO let an Admin blind-patch Status/
    // EmployeeId/BookingDate with no overlap re-check or transition validity check.
    public class UpdateBookingDto
    {
        public string? Notes { get; set; }
    }
}
