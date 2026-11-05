using System.Security.Cryptography;
using System.Text;
using ChefLan.Logic.Courses.Interfaces;

namespace ChefLan.Infrastructure.Services.Payments;

/// <summary>
/// Bound from the Payment configuration section (SePay Payment Gateway). All three secrets-and-ids belong in user-secrets
/// or environment variables (Payment__SePay__SecretKey), never in a committed appsettings file. The receiving bank
/// account is set in SePay's dashboard, not here.
/// Checkout is on only when everything needed is set; without it the website says "contact chef Lan".
/// </summary>
public sealed class PaymentOptions : IPaymentSettings
{
    public const string SectionName = "Payment";

    public string Provider => "SePay";

    /// <summary>Front of every order code. Letters and digits only.</summary>
    public string CodePrefix { get; set; } = "BB";

    /// <summary>How long an order stays payable. Long on purpose: people pay hours later.</summary>
    public int OrderLifetimeMinutes { get; set; } = 1440;

    public SePayOptions SePay { get; set; } = new();

    /// <summary>Filled from Web:PublicBaseUrl (the customer website), which the mails already need.</summary>
    public string SiteBaseUrl { get; set; } = string.Empty;

    public string MerchantId => SePay.MerchantId;

    public string SecretKey => SePay.SecretKey;

    public string CheckoutUrl => SePay.CheckoutUrl;

    public TimeSpan OrderLifetime => TimeSpan.FromMinutes(OrderLifetimeMinutes);

    public bool VerifyIpnSecret(string? presented)
    {
        if (string.IsNullOrEmpty(presented) || string.IsNullOrEmpty(SePay.IpnSecret))
        {
            return false;
        }

        // Hashed first so the comparison is fixed-length whatever was presented.
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(presented)),
            SHA256.HashData(Encoding.UTF8.GetBytes(SePay.IpnSecret)));
    }

    public bool CheckoutEnabled =>
        !string.IsNullOrWhiteSpace(SePay.MerchantId)
        && !string.IsNullOrWhiteSpace(SePay.SecretKey)
        && !string.IsNullOrWhiteSpace(SePay.IpnSecret)
        && Uri.TryCreate(SiteBaseUrl, UriKind.Absolute, out _)
        && Uri.TryCreate(SePay.CheckoutUrl, UriKind.Absolute, out _);
}

public sealed class SePayOptions
{
    /// <summary>"Mã đơn vị" on SePay's merchant information page (for example SP-LIVE-...).</summary>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>The merchant Secret Key from the same page. Signs the checkout form. Server-side only.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The secret set on SePay's IPN tab. SePay sends it as the X-Secret-Key header, and the notification endpoint
    /// refuses anything that does not carry it. A different secret from <see cref="SecretKey"/>.
    /// </summary>
    public string IpnSecret { get; set; } = string.Empty;

    /// <summary>Where the customer's browser POSTs the form. Production; check SePay's sandbox page for the test address.</summary>
    public string CheckoutUrl { get; set; } = "https://pay.sepay.vn/v1/checkout/init";
}
