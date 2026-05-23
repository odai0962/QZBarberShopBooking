using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace QZBarberShopBooking.Application.Helpers
{
    public static class UserContext
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static int? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("userId")?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                    return userId;
                return null;
            }
        }

        public static int? RoleId
        {
            get
            {
                var roleIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("roleId")?.Value;
                if (int.TryParse(roleIdClaim, out int roleId))
                    return roleId;
                return null;
            }
        }

        public static string? RoleName =>
            _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

        public static string? Email =>
            _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("email")?.Value;

        public static string? FullName
        {
            get
            {
                var firstName = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.GivenName)?.Value
                    ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("given_name")?.Value;
                var lastName = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Surname)?.Value
                    ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("family_name")?.Value;

                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                    return $"{firstName} {lastName}";

                return _httpContextAccessor?.HttpContext?.User?.FindFirst("fullName")?.Value;
            }
        }

        public static bool IsAuthenticated =>
            _httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public static bool IsInRole(string roleName) =>
            _httpContextAccessor?.HttpContext?.User?.IsInRole(roleName) ?? false;

        // Helper methods for common operations
        public static bool HasUserId() => UserId.HasValue;

        public static int GetUserIdOrThrow()
        {
            if (!UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated");
            return UserId.Value;
        }

        public static string GetUserEmailOrThrow()
        {
            var email = Email;
            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("User email not found");
            return email;
        }
    }
}