using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QZBarberShopBooking.Application.DTO.Shared;

namespace QZBarberShopBooking.API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new List<string>();

        if (!context.ModelState.IsValid)
        {
            errors.AddRange(
                context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage));
        }

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var validationResult = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            if (!validationResult.IsValid)
            {
                errors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(ApiResponse.Failure(errors, "Validation failed"));
            return;
        }

        await next();
    }
}
