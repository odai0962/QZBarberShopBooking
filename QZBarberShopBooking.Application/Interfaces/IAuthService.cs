using QZBarberShopBooking.Application.DTO.Auth;
using System.Threading;
using System.Threading.Tasks;

namespace QZBarberShopBooking.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RegisterEmployeeAsync(RegisterEmployeeDto registerDto, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> VerifyResetTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    }
}
