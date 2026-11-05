using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

/// <summary>Buying a course or a lesson by bank transfer: the customer's side, and staff's overrides.</summary>
public interface IOrderService
{
    CheckoutConfigResponse GetConfig();

    /// <summary>
    /// Starts a purchase. The price is worked out here, never taken from the caller. A still-payable order
    /// for the same item at the same price is returned instead of a new one. 409 if the customer already
    /// owns it; 503 when checkout is not configured.
    /// </summary>
    Task<Result<OrderResponse>> CreateAsync(long customerId, CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>The customer's own order, by code. Someone else's order is a 404.</summary>
    Task<Result<OrderResponse>> GetAsync(long customerId, string code, CancellationToken cancellationToken = default);

    Task<Result<OrderResponse>> CancelAsync(long customerId, string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> ListMineAsync(long customerId, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminOrderResponse>> ListAsync(OrderStatus? status, PagedRequest paging, CancellationToken cancellationToken = default);

    /// <summary>Staff confirm a payment by hand: grants the access and records who did it and why.</summary>
    Task<Result<AdminOrderResponse>> MarkPaidAsync(long orderId, MarkOrderPaidRequest request, long? staffId, CancellationToken cancellationToken = default);

    Task<Result<AdminOrderResponse>> CancelAsync(long orderId, CancellationToken cancellationToken = default);
}
