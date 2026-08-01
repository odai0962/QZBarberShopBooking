using QZBarberShopBooking.Application.DTO.Auth;

namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Login/refresh/logout — the "am I who I say I am" flows. Registration lives in
    /// <see cref="IRegistrationService"/> and password change/reset in
    /// <see cref="IPasswordResetService"/>: each has its own reason to change.</summary>
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);

        /// <summary>Logs in (or auto-registers, on first sign-in) a user authenticated via
        /// Google/Facebook through Firebase Auth. Issues the same access/refresh token pair as
        /// <see cref="LoginAsync"/>, so callers don't need to branch on how the user signed in.</summary>
        Task<AuthResponseDto> SocialLoginAsync(SocialLoginDto socialLoginDto, CancellationToken cancellationToken = default);

        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default);

        Task<bool> LogoutAsync(int userId, CancellationToken cancellationToken = default);
    }
}
