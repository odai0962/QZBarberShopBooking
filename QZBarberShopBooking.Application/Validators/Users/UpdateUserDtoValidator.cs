using FluentValidation;
using QZBarberShopBooking.Application.DTO.Users;

namespace QZBarberShopBooking.Application.Validators.Users
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().MaximumLength(50)
                .When(x => x.FirstName != null)
                .WithMessage("First name cannot be blank");

            RuleFor(x => x.LastName)
                .NotEmpty().MaximumLength(50)
                .When(x => x.LastName != null)
                .WithMessage("Last name cannot be blank");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[0-9+\-\s()]{10,20}$")
                .When(x => x.PhoneNumber != null)
                .WithMessage("Invalid phone number format");

            RuleFor(x => x.RoleId)
                .GreaterThan(0)
                .When(x => x.RoleId.HasValue)
                .WithMessage("RoleId must be a valid role");
        }
    }
}
