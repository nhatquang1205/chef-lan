using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

/// <summary>Bank transactions reported by the payment provider, and reconciling them with orders.</summary>
public interface IPaymentService
{
    /// <summary>
    /// Stores a SePay notification, then matches it to an order and pays the order when the rules say so.
    /// Safe to call twice with the same notification. Anything unreadable is stored for staff, never guessed at.
    /// </summary>
    Task<Result<PaymentIngestResponse>> IngestSePayAsync(string rawBody, CancellationToken cancellationToken = default);

    /// <summary>Null statuses = every transaction; pass the flagged ones to see what needs staff.</summary>
    Task<PagedResult<PaymentTransactionResponse>> ListAsync(
        IReadOnlyCollection<PaymentMatchStatus>? statuses,
        PagedRequest paging,
        CancellationToken cancellationToken = default);

    /// <summary>Staff attach a bank transaction to an order; an order that is not yet paid is paid and its access granted.</summary>
    Task<Result<PaymentTransactionResponse>> LinkAsync(long transactionId, long orderId, long? staffId, CancellationToken cancellationToken = default);
}
