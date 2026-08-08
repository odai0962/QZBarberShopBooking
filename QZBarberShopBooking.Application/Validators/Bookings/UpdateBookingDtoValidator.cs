using FluentValidation;
using QZBarberShopBooking.Application.DTO.Bookings;

namespace QZBarberShopBooking.Application.Validators.Bookings
{
    public class UpdateBookingDtoValidator : AbstractValidator<UpdateBookingDto>
    {
        public UpdateBookingDtoValidator()
        {
            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        }
    }
}
