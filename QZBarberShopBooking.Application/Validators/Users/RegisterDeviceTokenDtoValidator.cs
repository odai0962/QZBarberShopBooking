using FluentValidation;
using QZBarberShopBooking.Application.DTO.Users;

namespace QZBarberShopBooking.Application.Validators.Users
{
    public class RegisterDeviceTokenDtoValidator : AbstractValidator<RegisterDeviceTokenDto>
    {
        public RegisterDeviceTokenDtoValidator()
        {
            RuleFor(x => x.DeviceToken)
                .NotEmpty().WithMessage("A device token is required")
                .MaximumLength(500).WithMessage("Device token cannot exceed 500 characters");

            RuleFor(x => x.DeviceType)
                .MaximumLength(20).WithMessage("Device type cannot exceed 20 characters");
        }
    }
}
