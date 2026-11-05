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

/// <summary>Staff confirm a payment by hand. A reason is always required; a bank transaction is optional.</summary>
public sealed record MarkOrderPaidRequest(string Reason, long? PaymentTransactionId = null);

/// <summary>Staff attach a bank transaction to an order.</summary>
public sealed record LinkPaymentRequest(long OrderId);
