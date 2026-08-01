using QZBarberShopBooking.Application.DTO.Auth;

namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Login/refresh/logout — the "am I who I say I am" flows. Registration lives in
    /// <see cref="IRegistrationService"/> and password change/reset in
    /// <see cref="IPasswordResetService"/>: each has its own reason to change.</summary>
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);

        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default);

        Task<bool> LogoutAsync(int userId, CancellationToken cancellationToken = default);
    }
}
