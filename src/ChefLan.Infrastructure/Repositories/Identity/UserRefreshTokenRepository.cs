using ChefLan.DB.Contexts;
using ChefLan.Domain.Identity.Entities;
using ChefLan.Logic.Identity.Interfaces;

namespace ChefLan.Infrastructure.Repositories.Identity;

/// <summary>
/// SKELETON — not implemented yet. Arrives with the staff auth endpoints (step 2).
///
/// When implementing: look rows up by token_hash only — the raw token is never stored.
/// Rotation must set replaced_by_token_hash so a replayed token is detectable, and reuse
/// of an already-revoked token should revoke the whole chain for that user.
/// </summary>
internal sealed class UserRefreshTokenRepository(CoursesDbContext context) : IUserRefreshTokenRepository
{
    public Task<UserRefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRefreshTokenRepository.GetByHashAsync — step 2.");

    public Task<int> RevokeAllForUserAsync(
        long userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("UserRefreshTokenRepository.RevokeAllForUserAsync — step 2.");

    public void Add(UserRefreshToken token) => context.UserRefreshTokens.Add(token);
}
