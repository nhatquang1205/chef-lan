namespace ChefLan.Logic.Courses.Interfaces;

/// <summary>
/// SePay Payment Gateway settings, bound from configuration by Infrastructure. The receiving bank account is
/// configured in SePay's dashboard, not here. When anything below is missing checkout is off and the website
/// keeps its "contact chef Lan" note.
/// </summary>
public interface IPaymentSettings
{
    /// <summary>The payment provider's name, as stored on bank transactions.</summary>
    string Provider { get; }

    /// <summary>True when the merchant, both secrets and the website address are set. Orders cannot be created otherwise.</summary>
    bool CheckoutEnabled { get; }

    /// <summary>The merchant id ("Mã đơn vị") from SePay's dashboard.</summary>
    string MerchantId { get; }

    /// <summary>Signs the checkout form. Never leaves the server.</summary>
    string SecretKey { get; }

    /// <summary>Where the customer's browser POSTs the signed form.</summary>
    string CheckoutUrl { get; }

    /// <summary>The customer website's origin, for the pages SePay sends the customer back to.</summary>
    string SiteBaseUrl { get; }

    /// <summary>Letters at the front of every order code (the code is also the invoice number SePay echoes back).</summary>
    string CodePrefix { get; }

    TimeSpan OrderLifetime { get; }

    /// <summary>How long a contact order is held. Past it the customer is told so; staff can still approve it.</summary>
    TimeSpan ContactOrderLifetime { get; }

    /// <summary>
    /// Does the secret a notification presented match the IPN secret configured in SePay? Compared in constant
    /// time; false when none is configured.
    /// </summary>
    bool VerifyIpnSecret(string? presented);
}
