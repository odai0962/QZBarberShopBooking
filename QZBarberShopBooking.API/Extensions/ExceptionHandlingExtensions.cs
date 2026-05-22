using Microsoft.AspNetCore.Diagnostics;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Exceptions;

namespace QZBarberShopBooking.API.Extensions;

public static class ExceptionHandlingExtensions
{
    private const string GenericMessage = "An unexpected error occurred. Please try again later.";

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                var (statusCode, response) = MapException(exception, environment);

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response);
            });
        });

        return app;
    }

    private static (int StatusCode, ApiResponse<object> Response) MapException(
        Exception? exception,
        IWebHostEnvironment environment)
    {
        if (exception is AppException appEx)
        {
            var message = environment.IsDevelopment() ? appEx.Message : GetSafeMessage(appEx);
            var response = ApiResponse<object>.Failure(message, GetSafeMessage(appEx));

            if (exception is ValidationException validationEx)
            {
                response.Errors = validationEx.Errors
                    .SelectMany(e => e.Value)
                    .ToList();
            }

            if (environment.IsDevelopment())
            {
                response.Errors.Add(exception.Message);
                if (exception.StackTrace is not null)
                    response.Errors.Add(exception.StackTrace);
            }

            return (appEx.StatusCode, response);
        }

        var generic = ApiResponse<object>.Failure(
            environment.IsDevelopment() && exception != null ? exception.Message : GenericMessage,
            GenericMessage);

        if (exception != null && environment.IsDevelopment())
        {
            generic.Errors =
            [
                exception.Message,
                exception.StackTrace ?? string.Empty
            ];
        }

        return (StatusCodes.Status500InternalServerError, generic);
    }

    private static string GetSafeMessage(AppException exception) => exception switch
    {
        UnauthorizedException => "You are not authorized to perform this action.",
        ForbiddenException => "Access to this resource is forbidden.",
        NotFoundException => "The requested resource was not found.",
        ValidationException => "One or more validation errors occurred.",
        BusinessRuleException => exception.Message,
        _ => GenericMessage
    };
}
