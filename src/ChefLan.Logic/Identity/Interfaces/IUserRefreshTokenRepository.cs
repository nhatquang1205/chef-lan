using ChefLan.Domain.Identity.Entities;

namespace ChefLan.Logic.Identity.Interfaces;

public interface IUserRefreshTokenRepository
{
    Task<UserRefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<int> RevokeAllForUserAsync(long userId, DateTime revokedAt, CancellationToken cancellationToken = default);

    void Add(UserRefreshToken token);
}
