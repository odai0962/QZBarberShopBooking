using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using QZBarberShopBooking.Application.Helpers;
using System.Security.Claims;

namespace QZBarberShopBooking.API.Middleware
{
    public class AuthorizeMiddleware : Attribute, IAuthorizationFilter
    {
        private readonly IConfiguration _config;
        //private readonly ILoggerService _loggerService;

        public AuthorizeMiddleware(IConfiguration configuration
            )
        {
            _config = configuration;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Adding try catch here as Authorization filter run first, 
            // and Exception filter is not able to handler exception here
            try
            {
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();
                var httpMethod = context.HttpContext.Request.Method;


                var token = context.HttpContext.Request.Headers[HeaderNames.Authorization].FirstOrDefault()?.Split(" ").Last();
                if (token == null || (controllerName == "User" && actionName == "RefreshToken"))
                {
                    var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
                    if (allowAnonymous)
                        return;

                    throw new ArgumentNullException("Authorization Is Not Passed In Header");
                }
                var secretKey = _config.GetValue<string>("JWTSecretKey");
                var claims = JWTHelper.ValidateTokenWithLifeTime(token, secretKey);
                if (claims.Any())
                {
                    var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
                    context.HttpContext.User = user;
                }
                else
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            catch (Exception ex)
            {
                //  _loggerService.LogException(ex);
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
