namespace ChefLan.Domain.Enums;

public enum OrderPaidVia
{
    /// <summary>A bank transaction reported by the payment provider matched the order.</summary>
    Webhook = 1,

    /// <summary>Staff marked it paid by hand, or linked a bank transaction to it.</summary>
    Manual = 2,

    /// <summary>A free course: paid when created, nothing to collect.</summary>
    Free = 3,
}
