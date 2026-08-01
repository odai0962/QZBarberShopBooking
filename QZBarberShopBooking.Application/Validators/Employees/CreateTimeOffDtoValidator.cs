using FluentValidation;
using QZBarberShopBooking.Application.DTO.Employees;

namespace QZBarberShopBooking.Application.Validators.Employees
{
    public class CreateTimeOffDtoValidator : AbstractValidator<CreateTimeOffDto>
    {
        public CreateTimeOffDtoValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A reason is required")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");

            // Matches the EndDate >= StartDate check constraint in EmployeeTimeOffConfiguration.
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be on or after the start date");
        }
    }
}
