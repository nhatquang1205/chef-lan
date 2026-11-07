using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Contracts.Responses;

public sealed record PaymentFormField(string Name, string Value);

/// <summary>
/// How to pay: the browser POSTs <see cref="Fields"/> (already signed) to <see cref="ActionUrl"/>, which is SePay's hosted
/// payment page. Only present while the order can still be paid.
/// </summary>
public sealed record PaymentInstructionsResponse(decimal AmountVnd, string ActionUrl, IReadOnlyList<PaymentFormField> Fields);

public sealed record OrderItemResponse(
    OrderItemKind Kind,
    string Title,
    string CourseSlug,
    decimal ListPriceVnd,
    decimal DiscountVnd,
    decimal UnitPriceVnd,
    string? DiscountReason = null);

public sealed record StaffQuoteItemResponse(
    long CourseId,
    string Title,
    string Slug,
    CourseStatus Status,
    decimal ListPriceVnd,
    IReadOnlyList<QuoteDiscountResponse> Discounts,
    decimal SystemPriceVnd,
    bool AlreadyOwned);

/// <summary>The server's price for a staff order. Courses the customer already owns are flagged and left out of the subtotal.</summary>
public sealed record StaffOrderQuoteResponse(IReadOnlyList<StaffQuoteItemResponse> Items, decimal SubtotalVnd);

/// <summary>An order as its owner sees it. The status already accounts for expiry.</summary>
public sealed record OrderResponse
{
    public required string Code { get; init; }

    public required OrderStatus Status { get; init; }

    public required OrderPaymentMethod PaymentMethod { get; init; }

    public required decimal AmountVnd { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public DateTime? PaidAt { get; init; }

    public required IReadOnlyList<OrderItemResponse> Items { get; init; }

    /// <summary>Null once the order is no longer payable, and always null for a contact order.</summary>
    public PaymentInstructionsResponse? Payment { get; init; }
}

/// <summary>An order as staff see it, with who bought it and how it was paid.</summary>
public sealed record AdminOrderResponse
{
    public required long Id { get; init; }

    public required string Code { get; init; }

    public required OrderStatus Status { get; init; }

    public required OrderPaymentMethod PaymentMethod { get; init; }

    public required decimal AmountVnd { get; init; }

    /// <summary>Staff's discount on the whole order (already inside the item prices), if any.</summary>
    public decimal? DiscountVnd { get; init; }

    public string? DiscountReason { get; init; }

    public required long CustomerId { get; init; }

    public string? CustomerName { get; init; }

    public string? CustomerEmail { get; init; }

    public string? CustomerPhone { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public DateTime? PaidAt { get; init; }

    public OrderPaidVia? PaidVia { get; init; }

    public string? PaidReason { get; init; }

    public long? PaidByStaffId { get; init; }

    public long? PaymentTransactionId { get; init; }

    public required IReadOnlyList<OrderItemResponse> Items { get; init; }
}

public sealed record PaymentTransactionResponse
{
    public required long Id { get; init; }

    public required string Provider { get; init; }

    public required string ProviderTransactionId { get; init; }

    public required decimal AmountVnd { get; init; }

    public string? Content { get; init; }

    public string? Code { get; init; }

    public DateTime? TransactionDate { get; init; }

    public required DateTime ReceivedAt { get; init; }

    public long? OrderId { get; init; }

    public string? OrderCode { get; init; }

    public required PaymentMatchStatus MatchStatus { get; init; }

    public string? Note { get; init; }
}

/// <summary>What a payment notification turned into, for the provider's acknowledgement.</summary>
public sealed record PaymentIngestResponse(long TransactionId, PaymentMatchStatus MatchStatus, string? OrderCode);

/// <summary>Lets the website decide between "Buy now" and "contact chef Lan".</summary>
public sealed record CheckoutConfigResponse(bool Enabled);
