using ChefLan.Domain.Customers.Entities;

namespace ChefLan.Logic.Customers.Interfaces;

public interface ICustomerRefreshTokenRepository
{
    Task<CustomerRefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<int> RevokeAllForCustomerAsync(long customerId, DateTime revokedAt, CancellationToken cancellationToken = default);

    void Add(CustomerRefreshToken token);
}
