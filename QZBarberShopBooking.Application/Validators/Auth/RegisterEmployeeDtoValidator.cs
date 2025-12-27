using FluentValidation;
using QZBarberShopBooking.Application.DTO.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace QZBarberShopBooking.Application.Validators.Auth
{
    public class RegisterEmployeeDtoValidator : AbstractValidator<RegisterEmployeeDto>
    {
        public RegisterEmployeeDtoValidator()
        {
            Include(new RegisterDtoValidator());

            RuleFor(x => x.Specialization)
                .MaximumLength(100).WithMessage("Specialization cannot exceed 100 characters");

            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters");

            RuleFor(x => x.HourlyRate)
                .GreaterThan(0).When(x => x.HourlyRate.HasValue)
                .WithMessage("Hourly rate must be greater than 0");

            RuleFor(x => x.HireDate)
                .LessThanOrEqualTo(DateTime.Now).When(x => x.HireDate.HasValue)
                .WithMessage("Hire date cannot be in the future");
        }
    }
}
