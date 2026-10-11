namespace ChefLan.Commons.Auth;

/// <summary>
/// The caller, whichever JWT scheme authenticated them. "sub" is a staff user id under
/// Internal and a customer id under Customer, so there is deliberately NO bare Id: reading
/// one forces you to name the population you expect, and a mismatch returns false.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Null when the request is not authenticated.</summary>
    ActorType? Source { get; }

    bool IsAuthenticated { get; }

    string? Email { get; }

    /// <summary>Staff only. Always null for a customer.</summary>
    string? Role { get; }

    bool IsStaff { get; }

    bool IsCustomer { get; }

    /// <summary>False unless the caller is staff.</summary>
    bool TryGetStaffId(out long userId);

    /// <summary>False unless the caller is a customer.</summary>
    bool TryGetCustomerId(out long customerId);
}
