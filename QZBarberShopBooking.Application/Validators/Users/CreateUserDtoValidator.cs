using FluentValidation;
using QZBarberShopBooking.Application.DTO.Users;

namespace QZBarberShopBooking.Application.Validators.Users
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .MinimumLength(3).MaximumLength(50).WithMessage("Username must be 3-50 characters")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).MaximumLength(100).WithMessage("Password must be 6-100 characters");

            RuleFor(x => x.FirstName)
                .NotEmpty().MaximumLength(50).WithMessage("First name is required");

            RuleFor(x => x.LastName)
                .NotEmpty().MaximumLength(50).WithMessage("Last name is required");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required")
                .Matches(@"^[0-9+\-\s()]{10,20}$").WithMessage("Invalid phone number format");

            RuleFor(x => x.RoleId)
                .GreaterThan(0).WithMessage("A valid role must be selected");
        }
    }
}
