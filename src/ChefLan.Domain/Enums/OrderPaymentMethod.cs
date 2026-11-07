namespace ChefLan.Domain.Enums;

public enum OrderPaymentMethod
{
    /// <summary>Paid on the payment provider's hosted page; the notification pays the order.</summary>
    BankTransfer = 1,

    /// <summary>The customer arranges payment with chef Lan directly; staff approve the order by hand.</summary>
    ContactChef = 2,

    /// <summary>Staff created the order for a customer who arranged it with chef Lan; it is Paid from the start.</summary>
    StaffCreated = 3,
}
