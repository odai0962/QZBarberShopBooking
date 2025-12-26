using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace QZBarberShopBooking.API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception.InnerException is not null
                            ? exception.InnerException.ToString()
                            : exception.Message,
                Extensions = new Dictionary<string, object?>
            {
                {
                    "traceId",
                    Activity.Current?.Id ?? httpContext.TraceIdentifier
                }
            }
            };

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await httpContext.Response
                .WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }

}
