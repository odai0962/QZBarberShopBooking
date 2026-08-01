using FluentValidation;
using QZBarberShopBooking.Application.DTO.Bookings;

namespace QZBarberShopBooking.Application.Validators.Bookings
{
    public class CreateBookingServiceDtoValidator : AbstractValidator<CreateBookingServiceDto>
    {
        public CreateBookingServiceDtoValidator()
        {
            RuleFor(x => x.ServiceId)
                .GreaterThan(0).WithMessage("A valid service must be selected");
        }
    }

    public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
    {
        public CreateBookingDtoValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("A barber must be selected");

            RuleFor(x => x.Services)
                .NotEmpty().WithMessage("Select at least one service");

            RuleForEach(x => x.Services)
                .SetValidator(new CreateBookingServiceDtoValidator());

            RuleFor(x => x.DiscountPercentage)
                .InclusiveBetween(0, 100)
                .When(x => x.DiscountPercentage.HasValue)
                .WithMessage("Discount percentage must be between 0 and 100");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        }
    }
}
