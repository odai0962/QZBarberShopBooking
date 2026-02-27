using QZBarberShopBooking.Application.Helpers;

namespace QZBarberShopBooking.API.Middleware
{
    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextMiddleware(RequestDelegate next, IHttpContextAccessor httpContextAccessor)
        {
            _next = next;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!UserContextIsConfigured())
            {
                UserContext.Configure(_httpContextAccessor);
            }

            await _next(context);
        }

        private bool UserContextIsConfigured()
        {
            try
            {
                var test = UserContext.IsAuthenticated;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class UserContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserContextMiddleware>();
        }
    }
}
