using Microsoft.AspNetCore.Diagnostics;
using QZBarberShopBooking.Application.DTO.Shared;

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

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.Failure(
                    environment.IsDevelopment() && exception != null
                        ? exception.Message
                        : GenericMessage,
                    GenericMessage);

                if (exception != null && environment.IsDevelopment())
                {
                    response.Errors =
                    [
                        exception.Message,
                        exception.StackTrace ?? string.Empty
                    ];
                }

                await context.Response.WriteAsJsonAsync(response);
            });
        });

        return app;
    }
}
