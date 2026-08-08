using FluentValidation;
using QZBarberShopBooking.Application.DTO.Users;

namespace QZBarberShopBooking.Application.Validators.Users
{
    public class BlacklistUserDtoValidator : AbstractValidator<BlacklistUserDto>
    {
        public BlacklistUserDtoValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A reason is required")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }
    }
}
