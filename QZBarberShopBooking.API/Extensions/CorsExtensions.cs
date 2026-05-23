namespace QZBarberShopBooking.API.Extensions;

public static class CorsExtensions
{
    public const string DevelopmentPolicy = "Development";
    public const string ProductionPolicy = "Production";

    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(DevelopmentPolicy, policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            options.AddPolicy(ProductionPolicy, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => false);
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseApiCors(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        var policy = environment.IsDevelopment() ? DevelopmentPolicy : ProductionPolicy;
        return app.UseCors(policy);
    }
}
