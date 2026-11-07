using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Requests;

/// <summary>
/// Buy a course (by slug) or one lesson (by id). Exactly one of the two. <see cref="Method"/> is how the customer
/// will pay: on the provider's page, or by arranging it with chef Lan.
/// </summary>
public sealed record CreateOrderRequest(
    string? CourseSlug = null,
    long? LessonId = null,
    OrderPaymentMethod Method = OrderPaymentMethod.BankTransfer);

/// <summary>Staff's filters on the order list. <c>Status</c> is the effective status (Pending past its deadline is Expired).</summary>
public sealed record AdminOrderFilter
{
    public OrderStatus? Status { get; init; }

    public OrderPaymentMethod? PaymentMethod { get; init; }

    public long? CustomerId { get; init; }

    /// <summary>Matches the order code, or the customer's name, email or phone.</summary>
    public string? Q { get; init; }
}

/// <summary>Price a staff order before creating it. Nothing is saved.</summary>
public sealed record StaffOrderQuoteRequest(long CustomerId, IReadOnlyList<long> CourseIds);

/// <summary>One course on a staff order. <c>UnitPriceVnd</c> absent = the system price.</summary>
public sealed record StaffOrderItemRequest(long CourseId, decimal? UnitPriceVnd = null);

/// <summary>
/// An order staff create for a customer: paid and granted at once. <c>Note</c> (why / how it was paid) is required.
/// The order discount applies to the whole order and needs a reason.
/// </summary>
public sealed record CreateStaffOrderRequest(
    long CustomerId,
    IReadOnlyList<StaffOrderItemRequest> Items,
    string Note,
    decimal? OrderDiscountVnd = null,
    string? OrderDiscountReason = null);

/// <summary>Staff confirm a payment by hand. A reason is always required; a bank transaction is optional.</summary>
public sealed record MarkOrderPaidRequest(string Reason, long? PaymentTransactionId = null);

/// <summary>Staff attach a bank transaction to an order.</summary>
public sealed record LinkPaymentRequest(long OrderId);
