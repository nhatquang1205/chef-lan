using ChefLan.DB.Contexts;
using ChefLan.Domain.Identity.Entities;
using ChefLan.Logic.Identity.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Identity;

/// <summary>
/// Looks rows up by token_hash only — the raw token is never stored. The owning user is
/// included so a refresh can re-check is_active; a soft-deleted user is filtered out, which
/// makes the token unusable.
/// </summary>
internal sealed class UserRefreshTokenRepository(CoursesDbContext context) : IUserRefreshTokenRepository
{
    public Task<UserRefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        context.UserRefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<int> RevokeAllForUserAsync(
        long userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default) =>
        context.UserRefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, revokedAt), cancellationToken);

    public void Add(UserRefreshToken token) => context.UserRefreshTokens.Add(token);
}
