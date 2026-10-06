using ChefLan.DB.Contexts;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Interfaces;

namespace ChefLan.Infrastructure.Repositories.Customers;

/// <summary>
/// SKELETON — not implemented yet. Arrives with the customer auth endpoints (step 2).
/// Same rotation and reuse-detection rules as the staff token repository; only the table
/// and the (longer) lifetime differ.
/// </summary>
internal sealed class CustomerRefreshTokenRepository(CoursesDbContext context)
    : ICustomerRefreshTokenRepository
{
    public Task<CustomerRefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CustomerRefreshTokenRepository.GetByHashAsync — step 2.");

    public Task<int> RevokeAllForCustomerAsync(
        long customerId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CustomerRefreshTokenRepository.RevokeAllForCustomerAsync — step 2.");

    public void Add(CustomerRefreshToken token) => context.CustomerRefreshTokens.Add(token);
}
