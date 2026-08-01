namespace QZBarberShopBooking.Application.DTO.Auth
{
    /// <summary>Sent by the mobile app after it signs the user in with Google or Facebook via
    /// Firebase Auth. <see cref="IdToken"/> is the Firebase ID token (not the raw Google/Facebook
    /// provider token) — the backend verifies it once, the same way regardless of which provider
    /// the user picked.</summary>
    public class SocialLoginDto
    {
        public required string IdToken { get; set; }
    }
}
