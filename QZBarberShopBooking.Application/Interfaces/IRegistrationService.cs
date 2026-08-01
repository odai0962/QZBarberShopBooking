using QZBarberShopBooking.Application.DTO.Auth;

namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Account creation for the two roles the API can provision directly. See
    /// <see cref="IAuthenticationService"/> for login/refresh/logout.</summary>
    public interface IRegistrationService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);

        Task<AuthResponseDto> RegisterEmployeeAsync(RegisterEmployeeDto registerDto, CancellationToken cancellationToken = default);
    }
}
