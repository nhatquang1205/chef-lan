using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
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
    /// owns it; 503 when checkout is not configured and the method is BankTransfer. Asking again with another method
    /// switches the waiting order to it and keeps its code.
    /// </summary>
    Task<Result<OrderResponse>> CreateAsync(long customerId, CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>The customer's own order, by code. Someone else's order is a 404.</summary>
    Task<Result<OrderResponse>> GetAsync(long customerId, string code, CancellationToken cancellationToken = default);

    Task<Result<OrderResponse>> CancelAsync(long customerId, string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> ListMineAsync(long customerId, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminOrderResponse>> ListAsync(AdminOrderFilter filter, PagedRequest paging, CancellationToken cancellationToken = default);

    /// <summary>Staff confirm a payment by hand: grants the access and records who did it and why.</summary>
    Task<Result<AdminOrderResponse>> MarkPaidAsync(long orderId, MarkOrderPaidRequest request, long? staffId, CancellationToken cancellationToken = default);

    /// <summary>
    /// What the server would charge this customer for these courses, shared-module rule included. Nothing is saved. A course
    /// the customer already owns comes back flagged, with no discounts, and is left out of the subtotal.
    /// </summary>
    Task<Result<StaffOrderQuoteResponse>> QuoteForStaffAsync(StaffOrderQuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an order for a customer, already Paid, and grants every course in one transaction. The server recomputes the
    /// price; staff's per-course prices and order discount are applied on top. 409 if the customer already owns a course.
    /// </summary>
    Task<Result<AdminOrderResponse>> CreateByStaffAsync(CreateStaffOrderRequest request, long? staffId, CancellationToken cancellationToken = default);

    Task<Result<AdminOrderResponse>> CancelAsync(long orderId, CancellationToken cancellationToken = default);
}
