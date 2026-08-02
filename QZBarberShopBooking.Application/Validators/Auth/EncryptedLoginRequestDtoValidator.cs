using FluentValidation;
using QZBarberShopBooking.Application.DTO.Auth;

namespace QZBarberShopBooking.Application.Validators.Auth
{
    public class EncryptedLoginRequestDtoValidator : AbstractValidator<EncryptedLoginRequestDto>
    {
        public EncryptedLoginRequestDtoValidator()
        {
            RuleFor(x => x.Payload)
                .NotEmpty().WithMessage("Payload is required");
        }
    }
}
