using FluentValidation;
using QZBarberShopBooking.Application.DTO.Employees;

namespace QZBarberShopBooking.Application.Validators.Employees
{
    public class AssignEmployeeServicesDtoValidator : AbstractValidator<AssignEmployeeServicesDto>
    {
        public AssignEmployeeServicesDtoValidator()
        {
            RuleForEach(x => x.ServiceIds)
                .GreaterThan(0).WithMessage("Service ids must be valid");
        }
    }
}
