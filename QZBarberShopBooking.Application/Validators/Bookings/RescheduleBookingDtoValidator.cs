using FluentValidation;
using QZBarberShopBooking.Application.DTO.Bookings;

namespace QZBarberShopBooking.Application.Validators.Bookings
{
    public class RescheduleBookingDtoValidator : AbstractValidator<RescheduleBookingDto>
    {
        public RescheduleBookingDtoValidator()
        {
            RuleFor(x => x.RequestedStartUtc)
                .GreaterThan(DateTime.UtcNow).WithMessage("The new time must be in the future");
        }
    }
}
