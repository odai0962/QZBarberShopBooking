namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>The verified identity behind a social sign-in token (Firebase ID token issued
    /// after the mobile app signs in with Google or Facebook).</summary>
    public record SocialAuthPrincipal(string ProviderUserId, string Email, bool EmailVerified, string? DisplayName);

    public interface ISocialAuthTokenVerifier
    {
        Task<SocialAuthPrincipal> VerifyAsync(string idToken, CancellationToken cancellationToken = default);
    }
}
