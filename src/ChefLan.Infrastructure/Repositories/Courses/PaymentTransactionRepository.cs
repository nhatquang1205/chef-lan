using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class PaymentTransactionRepository(CoursesDbContext context) : IPaymentTransactionRepository
{
    public Task<PaymentTransaction?> GetByProviderIdAsync(string provider, string providerTransactionId, CancellationToken cancellationToken = default) =>
        context.PaymentTransactions.FirstOrDefaultAsync(
            p => p.Provider == provider && p.ProviderTransactionId == providerTransactionId,
            cancellationToken);

    public Task<PaymentTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.PaymentTransactions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<PagedResult<PaymentTransaction>> ListAsync(
        IReadOnlyCollection<PaymentMatchStatus>? statuses,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var filter = statuses?.ToList();
        var query = context.PaymentTransactions
            .AsNoTracking()
            .Where(p => filter == null || filter.Contains(p.MatchStatus));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(p => p.Order)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<PaymentTransaction>.Create(items, paging.Page, paging.PageSize, total);
    }

    public void Add(PaymentTransaction transaction) => context.PaymentTransactions.Add(transaction);
}
