using FluentValidation;
using QZBarberShopBooking.Application.DTO.Auth;

namespace QZBarberShopBooking.Application.Validators.Auth
{
    public class SocialLoginDtoValidator : AbstractValidator<SocialLoginDto>
    {
        public SocialLoginDtoValidator()
        {
            RuleFor(x => x.IdToken).NotEmpty();
        }
    }
}
