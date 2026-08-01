using FluentValidation;
using QZBarberShopBooking.Application.DTO.Employees;

namespace QZBarberShopBooking.Application.Validators.Employees
{
    public class UpdateEmployeeDtoValidator : AbstractValidator<UpdateEmployeeDto>
    {
        public UpdateEmployeeDtoValidator()
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

            RuleFor(x => x.Specialization)
                .MaximumLength(100).WithMessage("Specialization cannot exceed 100 characters");

            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters");

            RuleFor(x => x.HourlyRate)
                .GreaterThan(0)
                .When(x => x.HourlyRate.HasValue)
                .WithMessage("Hourly rate must be greater than zero");
        }
    }
}
