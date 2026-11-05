using System.Security.Cryptography;
using System.Text;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;

namespace ChefLan.Logic.Courses.Payments;

/// <summary>
/// The signed form that sends the customer to SePay's hosted payment page. The browser POSTs it, so the field values
/// are visible, but without the secret nobody can change the amount: SePay recomputes the signature.
/// </summary>
public static class PaymentInstructions
{
    /// <summary>
    /// The fields SePay signs, in the order it expects them. Empty ones are left out of the signature, and the form keeps
    /// its own order (see <see cref="For"/>). Source: SePay's "Create payment order" page.
    /// </summary>
    internal static readonly string[] SignedFields =
    [
        "order_amount", "merchant", "currency", "operation", "order_description", "order_invoice_number",
        "customer_id", "payment_method", "success_url", "error_url", "cancel_url",
    ];

    public static PaymentInstructionsResponse For(IPaymentSettings settings, string code, decimal amountVnd)
    {
        var page = $"{settings.SiteBaseUrl.TrimEnd('/')}/checkout/{Uri.EscapeDataString(code)}";

        // Form order follows SePay's sample form; the signature does not depend on it, but SePay asks to keep it.
        var values = new List<KeyValuePair<string, string>>
        {
            new("merchant", settings.MerchantId),
            new("currency", "VND"),
            new("order_amount", decimal.Truncate(amountVnd).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("operation", "PURCHASE"),
            new("order_description", $"Bon Bocon {code}"),
            new("order_invoice_number", code),
            new("payment_method", "BANK_TRANSFER"),
            new("success_url", $"{page}?result=success"),
            new("error_url", $"{page}?result=error"),
            new("cancel_url", $"{page}?result=cancel"),
        };

        var signature = Sign(values, settings.SecretKey);
        values.Add(new("signature", signature));

        return new PaymentInstructionsResponse(
            decimal.Truncate(amountVnd),
            settings.CheckoutUrl,
            values.Select(v => new PaymentFormField(v.Key, v.Value)).ToList());
    }

    /// <summary>
    /// HMAC-SHA256, Base64, over <c>name=value</c> pairs joined by commas, in SePay's fixed field order, skipping empty values.
    /// </summary>
    internal static string Sign(IReadOnlyCollection<KeyValuePair<string, string>> values, string secretKey)
    {
        var byName = values.ToDictionary(v => v.Key, v => v.Value);
        var parts = SignedFields
            .Where(name => byName.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value) && value != "0")
            .Select(name => $"{name}={byName[name]}");

        return Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secretKey), Encoding.UTF8.GetBytes(string.Join(',', parts))));
    }
}
