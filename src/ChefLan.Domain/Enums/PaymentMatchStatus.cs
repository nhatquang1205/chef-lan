namespace ChefLan.Domain.Enums;

/// <summary>What reconciliation made of one bank transaction. Anything but Matched needs no action unless noted.</summary>
public enum PaymentMatchStatus
{
    /// <summary>No order code in the transfer note, or the code is unknown. Staff can link it to an order.</summary>
    Unmatched = 1,

    /// <summary>Exact amount: the order was paid.</summary>
    Matched = 2,

    /// <summary>More than the order total: the order was paid, the difference is for staff to refund.</summary>
    Overpaid = 3,

    /// <summary>Less than the order total: the order stays Pending, staff decide.</summary>
    Underpaid = 4,

    /// <summary>The order had expired or been cancelled. Not granted automatically; staff link it or refund.</summary>
    LateAfterExpiry = 5,

    /// <summary>The order was already paid: a second transfer, for staff to refund.</summary>
    Duplicate = 6,

    /// <summary>Not an incoming transfer, or not a positive amount. Stored for the record only.</summary>
    Ignored = 7,
}
