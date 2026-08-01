using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using QZBarberShopBooking.Application.Exceptions;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Auth
{
    /// <summary>Verifies the Firebase ID token the mobile app forwards after a Google/Facebook
    /// sign-in. Firebase itself already checked the token's signature/issuer/expiry against the
    /// project registered in <c>Firebase:ServiceAccountKeyPath</c> (see Program.cs) — this class
    /// just calls that check and translates the result into the claims the rest of the app needs.</summary>
    public class FirebaseSocialAuthTokenVerifier : ISocialAuthTokenVerifier, IScopedService
    {
        private readonly ILogger<FirebaseSocialAuthTokenVerifier> _logger;

        public FirebaseSocialAuthTokenVerifier(ILogger<FirebaseSocialAuthTokenVerifier> logger)
        {
            _logger = logger;
        }

        public async Task<SocialAuthPrincipal> VerifyAsync(string idToken, CancellationToken cancellationToken = default)
        {
            if (FirebaseApp.DefaultInstance == null)
                throw new InvalidOperationException(
                    "Social login is not configured on this server (Firebase:ServiceAccountKeyPath is missing).");

            FirebaseToken decoded;
            try
            {
                decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, cancellationToken);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning(ex, "Rejected an invalid/expired Firebase sign-in token");
                throw new UnauthorizedException("Invalid or expired sign-in token");
            }

            var email = decoded.Claims.TryGetValue("email", out var emailClaim) ? emailClaim?.ToString() : null;
            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedException("The selected sign-in provider did not share an email address");

            var emailVerified = decoded.Claims.TryGetValue("email_verified", out var verifiedClaim)
                && verifiedClaim is bool isVerified && isVerified;

            var name = decoded.Claims.TryGetValue("name", out var nameClaim) ? nameClaim?.ToString() : null;

            return new SocialAuthPrincipal(decoded.Uid, email, emailVerified, name);
        }
    }
}
