using QZBarberShopBooking.Application.DTO.Users;

namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Self-service profile operations for the currently authenticated user. See
    /// <see cref="IUserAdminService"/> for admin-only user management — kept separate so
    /// ProfileController (any authenticated user) doesn't depend on operations only
    /// UsersController (Admin-only) ever calls.</summary>
    public interface IUserProfileService
    {
        Task<UserDto> GetCurrentUserProfile();

        Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto, CancellationToken cancellationToken = default);
    }
}
