namespace ChefLan.Domain.Enums;

/// <summary>
/// Expiry is lazy: a Pending order past its ExpiresAt is treated as Expired when it is read or matched,
/// so <see cref="Expired"/> is only stored if something chooses to persist it.
/// </summary>
public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Expired = 3,
    Cancelled = 4,
}
