using ChefLan.Domain.Common;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

/// <summary>
/// A customer's request to buy a course or a single lesson, paid on the provider's page (bank transfer) or arranged with chef Lan (contact). The
/// <see cref="Code"/> goes in the transfer note and is how a bank transaction finds its order.
/// The amount is fixed from the server-side quote when the order is created.
///
/// Not soft-deletable: it is a money record. The audit of how it was paid lives here (PaidVia, PaidReason,
/// PaidByStaffId, PaymentTransactionId), not in a separate event table.
/// </summary>
public class Order : AuditableEntity, IHasRowVersion
{
    public required string Code { get; set; }

    public long CustomerId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public OrderPaymentMethod PaymentMethod { get; set; } = OrderPaymentMethod.BankTransfer;

    public decimal AmountVnd { get; set; }

    /// <summary>Staff's discount on the whole order, already shared out over the items. Null on every other order.</summary>
    public decimal? DiscountVnd { get; set; }

    public string? DiscountReason { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public OrderPaidVia? PaidVia { get; set; }

    /// <summary>Why staff marked it paid by hand. Always set when <see cref="PaidVia"/> is Manual.</summary>
    public string? PaidReason { get; set; }

    /// <summary>Which staff account paid it by hand. An id only, no foreign key.</summary>
    public long? PaidByStaffId { get; set; }

    /// <summary>The bank transaction that paid it, if there is one.</summary>
    public long? PaymentTransactionId { get; set; }

    public uint RowVersion { get; set; }

    public Customer? Customer { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    /// <summary>Expiry is lazy: a Pending order past its deadline reads as Expired.</summary>
    public OrderStatus EffectiveStatus(DateTime utcNow) =>
        Status == OrderStatus.Pending && utcNow >= ExpiresAt ? OrderStatus.Expired : Status;
}
