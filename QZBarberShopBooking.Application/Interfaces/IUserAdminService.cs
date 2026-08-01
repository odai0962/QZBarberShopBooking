using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.DTO.Users;

namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Admin-only user management. See <see cref="IUserProfileService"/> for the
    /// self-service operations any authenticated user can call.</summary>
    public interface IUserAdminService
    {
        Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<PaginatedResponse<UserDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

        Task<UserDto> CreateAsync(CreateUserDto createUserDto, CancellationToken cancellationToken = default);

        Task<UserDto> UpdateAsync(int id, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);
    }
}
