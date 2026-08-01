namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Changing a known password and resetting a forgotten one. Separate from
    /// <see cref="IAuthenticationService"/>/<see cref="IRegistrationService"/> since a change to
    /// reset-token policy has no business touching login or registration.</summary>
    public interface IPasswordResetService
    {
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);

        Task<bool> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);

        Task<bool> VerifyResetTokenAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    }
}
