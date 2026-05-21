using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using QZBarberShopBooking.Application.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace QZBarberShopBooking.API.Middleware
{
    public class AuthorizeMiddleware : Attribute, IAuthorizationFilter
    {
        // Allow parameterless attribute usage. Resolve IConfiguration via RequestServices at runtime.
        public AuthorizeMiddleware() { }

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
                var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var secretKey = config.GetValue<string>("JwtSettings:SecretKey");
                var issuer = config.GetValue<string>("JwtSettings:Issuer");
                var audience = config.GetValue<string>("JwtSettings:Audience");
                if (string.IsNullOrWhiteSpace(issuer)) issuer = null;
                if (string.IsNullOrWhiteSpace(audience)) audience = null;
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
                ClaimsPrincipal principal;
                try
                {
                    principal = JWTHelper.ValidateTokenWithLifeTime(token, secretKey, issuer, audience);
                    context.HttpContext.User = principal;
                }
                catch
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
