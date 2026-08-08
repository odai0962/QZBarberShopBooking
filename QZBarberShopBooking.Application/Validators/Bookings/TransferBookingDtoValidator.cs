using FluentValidation;
using QZBarberShopBooking.Application.DTO.Bookings;

namespace QZBarberShopBooking.Application.Validators.Bookings
{
    public class TransferBookingDtoValidator : AbstractValidator<TransferBookingDto>
    {
        public TransferBookingDtoValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("A barber must be selected");
        }
    }
}
