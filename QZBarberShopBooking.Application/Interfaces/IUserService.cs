using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.DTO.Users;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetByIdAsync(int id,CancellationToken cancellationToken = default);

        Task<IEnumerable<UserDto>> GetAllAsync( CancellationToken cancellationToken = default);

        Task<PaginatedResponse<UserDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

        Task<UserDto> CreateAsync( CreateUserDto createUserDto,CancellationToken cancellationToken = default);

        Task<UserDto> UpdateAsync(int id, UpdateUserDto updateUserDto,CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id,CancellationToken cancellationToken = default);

        Task<bool> ToggleStatusAsync(int id,CancellationToken cancellationToken = default);

        Task<UserProfileDto> GetProfileAsync(int userId,CancellationToken cancellationToken = default);

        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto,CancellationToken cancellationToken = default);
        Task<UserDto> GetCurrentUserProfile();
    }
}
