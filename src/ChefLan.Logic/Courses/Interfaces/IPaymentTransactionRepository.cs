using ChefLan.Commons.Pagination;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Interfaces;

public interface IPaymentTransactionRepository
{
    /// <summary>Tracked.</summary>
    Task<PaymentTransaction?> GetByProviderIdAsync(string provider, string providerTransactionId, CancellationToken cancellationToken = default);

    /// <summary>Tracked.</summary>
    Task<PaymentTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Read-only, with the order loaded for its code. Newest first. Null statuses = all.</summary>
    Task<PagedResult<PaymentTransaction>> ListAsync(
        IReadOnlyCollection<PaymentMatchStatus>? statuses,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    void Add(PaymentTransaction transaction);
}
