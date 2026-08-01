using FluentValidation;
using QZBarberShopBooking.Application.DTO.Users;

namespace QZBarberShopBooking.Application.Validators.Users
{
    public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
    {
        public UpdateProfileDtoValidator()
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

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.UtcNow)
                .When(x => x.DateOfBirth.HasValue)
                .WithMessage("Date of birth must be in the past");
        }
    }
}
