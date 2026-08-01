using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QZBarberShopBooking.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "Auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");
        var permitLimit = section.GetValue("PermitLimit", 100);
        var windowMinutes = section.GetValue("Window", 1);
        var queueLimit = section.GetValue("QueueLimit", 0);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partitioned per client IP so one abusive caller can't exhaust the quota for
            // everyone else — a single shared/global window would do that.
            options.AddPolicy(AuthPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(windowMinutes),
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
        });

        return services;
    }
}
