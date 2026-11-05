using System.Globalization;
using System.Text.Json;

namespace ChefLan.Logic.Courses.Payments;

/// <summary>One payment notification from SePay's Payment Gateway.</summary>
public sealed record SePayNotification(
    string TransactionId,
    decimal Amount,
    /// <summary>True for a successful payment (ORDER_PAID, approved); false for a void or anything not yet money in.</summary>
    bool IsPayment,
    string NotificationType,
    /// <summary>Our order code: the invoice number we sent when the customer was sent to SePay.</summary>
    string? InvoiceNumber,
    string? Description,
    DateTime? TransactionDate);

/// <summary>
/// Reads the body SePay's Payment Gateway sends to the IPN URL: <c>notification_type</c> plus <c>order</c>, <c>transaction</c> and
/// <c>customer</c> objects. ALL of the reading is here, so a difference in the real payload is a one-file fix. Anything
/// unreadable returns null and the caller keeps the raw body for staff, never guessing an amount.
/// Source: SePay's "IPN" page; the transaction object's own documentation is incomplete, so unknown fields are ignored.
/// </summary>
public static class SePayIpnParser
{
    // SePay reports local time without an offset.
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static SePayNotification? Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var type = Text(root, "notification_type");
            var order = Child(root, "order");
            var transaction = Child(root, "transaction");

            if (type is null || order is null)
            {
                return null;
            }

            // The bank's own reference is the stable key; the gateway's row id is the fallback.
            var id = Text(transaction, "transaction_id") ?? Text(transaction, "id") ?? Text(order, "order_id");
            var amount = Number(transaction, "transaction_amount") ?? Number(order, "order_amount");

            if (id is null || amount is null)
            {
                return null;
            }

            var status = Text(transaction, "transaction_status");
            var approved = status is null || status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase);
            var captured = Text(order, "order_status") is not { } orderStatus || orderStatus.Equals("CAPTURED", StringComparison.OrdinalIgnoreCase);

            return new SePayNotification(
                id,
                amount.Value,
                type.Equals("ORDER_PAID", StringComparison.OrdinalIgnoreCase) && approved && captured,
                type,
                Text(order, "order_invoice_number"),
                Text(order, "order_description"),
                Date(Text(transaction, "transaction_date")));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement? Child(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var child) && child.ValueKind == JsonValueKind.Object ? child : null;

    private static string? Text(JsonElement? parent, string name)
    {
        if (parent is { } element && element.TryGetProperty(name, out var value))
        {
            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }
        }

        return null;
    }

    private static decimal? Number(JsonElement? parent, string name)
    {
        if (parent is not { } element || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        // "50000.00" arrives as a string in the order object.
        return value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
    }

    private static DateTime? Date(string? text) =>
        DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)
            ? DateTime.SpecifyKind(local - VietnamOffset, DateTimeKind.Utc)
            : null;
}
