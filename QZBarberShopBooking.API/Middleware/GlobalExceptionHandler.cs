using Microsoft.AspNetCore.Diagnostics;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Exceptions;

namespace QZBarberShopBooking.API.Middleware
{
    // Client responses never carry exception.Message/StackTrace, in any environment — only a
    // curated, safe message per exception type. Full detail still goes to ILogger below, which is
    // the only place it should ever be visible.
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private const string GenericMessage = "An unexpected error occurred. Please try again later.";

        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            var (statusCode, response) = MapException(exception);

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }

        private static (int StatusCode, ApiResponse<object> Response) MapException(Exception exception)
        {
            if (exception is AppException appEx)
            {
                var message = GetSafeMessage(appEx);
                var response = ApiResponse<object>.Failure(message, message);

                if (exception is ValidationException validationEx)
                {
                    response.Errors = validationEx.Errors
                        .SelectMany(e => e.Value)
                        .ToList();
                }

                return (appEx.StatusCode, response);
            }

            return (StatusCodes.Status500InternalServerError, ApiResponse<object>.Failure(GenericMessage, GenericMessage));
        }

        private static string GetSafeMessage(AppException exception) => exception switch
        {
            UnauthorizedException => "You are not authorized to perform this action.",
            ForbiddenException => "Access to this resource is forbidden.",
            NotFoundException => "The requested resource was not found.",
            ValidationException => "One or more validation errors occurred.",
            BusinessRuleException => exception.Message,
            ConflictException => exception.Message,
            _ => GenericMessage
        };
    }
}
