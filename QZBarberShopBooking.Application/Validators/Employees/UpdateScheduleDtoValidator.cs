using FluentValidation;
using QZBarberShopBooking.Application.DTO.Employees;

namespace QZBarberShopBooking.Application.Validators.Employees
{
    public class UpdateScheduleDayDtoValidator : AbstractValidator<UpdateScheduleDayDto>
    {
        public UpdateScheduleDayDtoValidator()
        {
            // Matches the EndTime > StartTime check constraint in EmployeeScheduleConfiguration.
            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .When(x => x.IsWorkingDay)
                .WithMessage("End time must be after start time");

            RuleFor(x => x.BreakEndTime)
                .GreaterThan(x => x.BreakStartTime)
                .When(x => x.BreakStartTime.HasValue && x.BreakEndTime.HasValue)
                .WithMessage("Break end time must be after break start time");
        }
    }

    public class UpdateScheduleDtoValidator : AbstractValidator<UpdateScheduleDto>
    {
        public UpdateScheduleDtoValidator()
        {
            RuleFor(x => x.ScheduleDays)
                .NotEmpty().WithMessage("At least one schedule day is required");

            RuleForEach(x => x.ScheduleDays)
                .SetValidator(new UpdateScheduleDayDtoValidator());
        }
    }
}
