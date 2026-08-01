using QZBarberShopBooking.Application.DTO.Auth;
using QZBarberShopBooking.Domain.Entities;

namespace QZBarberShopBooking.Application.Interfaces
{
    /// <summary>Issues access/refresh tokens for an already-authenticated or newly-created user
    /// and persists the (hashed) refresh token. Shared by login, registration, and token-refresh
    /// flows so token issuance lives in exactly one place.</summary>
    public interface ITokenService
    {
        Task<AuthResponseDto> IssueTokensAsync(User user, CancellationToken cancellationToken = default);
    }
}
