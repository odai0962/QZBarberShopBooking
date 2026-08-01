using FluentValidation;
using QZBarberShopBooking.Application.DTO.Services;

namespace QZBarberShopBooking.Application.Validators.Services
{
    public class UpdateServiceDtoValidator : AbstractValidator<UpdateServiceDto>
    {
        public UpdateServiceDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().MaximumLength(100)
                .When(x => x.Name != null)
                .WithMessage("Name cannot be blank");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.BasePrice)
                .GreaterThan(0)
                .When(x => x.BasePrice.HasValue)
                .WithMessage("Base price must be greater than zero");

            RuleFor(x => x.DefaultDuration)
                .GreaterThan(TimeSpan.Zero)
                .When(x => x.DefaultDuration.HasValue)
                .WithMessage("Duration must be greater than zero");

            RuleFor(x => x.Category)
                .MaximumLength(50).WithMessage("Category cannot exceed 50 characters");
        }
    }
}
