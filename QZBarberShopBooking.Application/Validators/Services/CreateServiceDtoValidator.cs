using FluentValidation;
using QZBarberShopBooking.Application.DTO.Services;

namespace QZBarberShopBooking.Application.Validators.Services
{
    public class CreateServiceDtoValidator : AbstractValidator<CreateServiceDto>
    {
        public CreateServiceDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.BasePrice)
                .GreaterThan(0).WithMessage("Base price must be greater than zero");

            RuleFor(x => x.DefaultDuration)
                .GreaterThan(TimeSpan.Zero).WithMessage("Duration must be greater than zero");

            RuleFor(x => x.Category)
                .MaximumLength(50).WithMessage("Category cannot exceed 50 characters");
        }
    }
}
