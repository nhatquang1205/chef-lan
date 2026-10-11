using ChefLan.DB.Contexts;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Customers;

/// <summary>Same rules as the staff token repository; only the table and lifetime differ.</summary>
internal sealed class CustomerRefreshTokenRepository(CoursesDbContext context)
    : ICustomerRefreshTokenRepository
{
    public Task<CustomerRefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        context.CustomerRefreshTokens
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<int> RevokeAllForCustomerAsync(
        long customerId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default) =>
        context.CustomerRefreshTokens
            .Where(t => t.CustomerId == customerId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, revokedAt), cancellationToken);

    public void Add(CustomerRefreshToken token) => context.CustomerRefreshTokens.Add(token);
}
