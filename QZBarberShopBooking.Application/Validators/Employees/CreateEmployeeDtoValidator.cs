using FluentValidation;
using QZBarberShopBooking.Application.DTO.Employees;
using QZBarberShopBooking.Application.Validators.Auth;

namespace QZBarberShopBooking.Application.Validators.Employees
{
    public class CreateEmployeeDtoValidator : AbstractValidator<CreateEmployeeDto>
    {
        public CreateEmployeeDtoValidator()
        {
            Include(new RegisterEmployeeDtoValidator());

            RuleFor(x => x.HourlyRate)
                .GreaterThan(0)
                .When(x => x.HourlyRate.HasValue)
                .WithMessage("Hourly rate must be greater than zero");

            RuleFor(x => x.HireDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.HireDate.HasValue)
                .WithMessage("Hire date cannot be in the future");
        }
    }
}
