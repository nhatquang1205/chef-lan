using ChefLan.Domain.Common;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

/// <summary>
/// One bank transaction reported by a payment provider. Every notification is stored, matched or not, so
/// reconciliation never loses money. (<see cref="Provider"/>, <see cref="ProviderTransactionId"/>) is unique: it is the
/// idempotency key, so a retried notification cannot be applied twice.
/// </summary>
public class PaymentTransaction : AuditableEntity, IHasRowVersion
{
    public required string Provider { get; set; }

    public required string ProviderTransactionId { get; set; }

    public decimal AmountVnd { get; set; }

    /// <summary>The transfer note as the bank delivered it.</summary>
    public string? Content { get; set; }

    /// <summary>The order code found in the note or sent by the provider, if any.</summary>
    public string? Code { get; set; }

    public string? AccountNumber { get; set; }

    /// <summary>When the bank booked it, as the provider reports it.</summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>The notification body exactly as received.</summary>
    public string? RawPayload { get; set; }

    public long? OrderId { get; set; }

    public PaymentMatchStatus MatchStatus { get; set; } = PaymentMatchStatus.Unmatched;

    /// <summary>A line on what staff did or why it was flagged.</summary>
    public string? Note { get; set; }

    public uint RowVersion { get; set; }

    public Order? Order { get; set; }
}
