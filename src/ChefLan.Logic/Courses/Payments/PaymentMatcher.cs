using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Payments;

/// <summary>What reconciliation decided about one incoming bank transaction.</summary>
public readonly record struct MatchDecision(PaymentMatchStatus Status, bool PayOrder);

/// <summary>
/// The reconciliation rules between an order and a bank transaction, as a pure function so every case is
/// testable without a database. The order is whatever the transfer note named, or null.
/// </summary>
public static class PaymentMatcher
{
    public static MatchDecision Decide(Order? order, decimal amount, DateTime utcNow)
    {
        if (order is null)
        {
            return new(PaymentMatchStatus.Unmatched, false);
        }

        var status = order.EffectiveStatus(utcNow);

        return status switch
        {
            OrderStatus.Paid => new(PaymentMatchStatus.Duplicate, false),
            OrderStatus.Expired or OrderStatus.Cancelled => new(PaymentMatchStatus.LateAfterExpiry, false),
            _ when amount < order.AmountVnd => new(PaymentMatchStatus.Underpaid, false),
            _ when amount > order.AmountVnd => new(PaymentMatchStatus.Overpaid, true),
            _ => new(PaymentMatchStatus.Matched, true),
        };
    }
}
