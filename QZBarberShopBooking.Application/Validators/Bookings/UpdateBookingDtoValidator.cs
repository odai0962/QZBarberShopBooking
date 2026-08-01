using FluentValidation;
using QZBarberShopBooking.Application.DTO.Bookings;

namespace QZBarberShopBooking.Application.Validators.Bookings
{
    public class UpdateBookingDtoValidator : AbstractValidator<UpdateBookingDto>
    {
        public UpdateBookingDtoValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0)
                .When(x => x.EmployeeId.HasValue)
                .WithMessage("EmployeeId must be a valid id");

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage("Status must be a valid booking status");

            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        }
    }
}
