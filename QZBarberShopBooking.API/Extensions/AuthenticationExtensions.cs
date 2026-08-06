using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QZBarberShopBooking.Application.DTO.Shared;
using System.Text;

namespace QZBarberShopBooking.API.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment = false)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");

            var secretKey = jwtSettings["SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    "JWT Secret Key is not configured. For Development, run: dotnet user-secrets set \"JwtSettings:SecretKey\" \"<your-key>\" --project QZBarberShopBooking.API. " +
                    "For Production, set environment variable JwtSettings__SecretKey.");
            }

            var issuer = jwtSettings["Issuer"] ?? "QZBarberShop_API";
            var audience = jwtSettings["Audience"] ?? "QZBarberShop_Client";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !isDevelopment;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5) 
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogInformation("Token validated successfully");
                        return Task.CompletedTask;
                    },
                    // [Authorize] rejections never throw — the authorization middleware
                    // short-circuits with an empty body before GlobalExceptionHandler ever gets
                    // a chance to run, so without these two handlers a 401/403 goes to the client
                    // with no JSON at all (Dio's/the client's own generic error text stands in
                    // for a real message). Missing/invalid token -> OnChallenge (401); valid
                    // token but wrong role -> OnForbidden (403). Same ApiResponse envelope as
                    // every other error response in the API.
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        const string message = "Authentication required.";
                        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Failure(message, message));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        const string message = "You are not authorized to perform this action.";
                        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Failure(message, message));
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}