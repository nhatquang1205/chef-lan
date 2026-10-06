namespace ChefLan.Commons.Auth;

/// <summary>
/// The signed-in STAFF user. Separate from <see cref="ICurrentCustomerContext"/> on
/// purpose: there is deliberately no single "current principal" abstraction, because
/// "sub" means different things under the two schemes and one shared accessor is exactly
/// how those two ids get confused.
/// </summary>
public interface ICurrentStaffContext
{
    bool IsAuthenticated { get; }

    long? UserId { get; }

    string? Email { get; }

    string? Role { get; }

    bool IsAdmin { get; }

    /// <summary>Throws when no staff token is present.</summary>
    long RequireUserId();
}
