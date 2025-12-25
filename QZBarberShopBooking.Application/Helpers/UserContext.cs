using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace QZBarberShopBooking.Application.Helpers
{
    public static class UserContext
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static int UserId =>
            int.Parse(_httpContextAccessor?.HttpContext?.User?.FindFirst("userId")?.Value);

        public static string? RoleId =>
            _httpContextAccessor?.HttpContext?.User?.FindFirst("roleId")?.Value;

        public static string? RoleName =>
            _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
