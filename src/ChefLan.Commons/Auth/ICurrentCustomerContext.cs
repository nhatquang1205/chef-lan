namespace ChefLan.Commons.Auth;

/// <summary>The signed-in CUSTOMER. See the note on <see cref="ICurrentStaffContext"/>.</summary>
public interface ICurrentCustomerContext
{
    bool IsAuthenticated { get; }

    long? CustomerId { get; }

    string? Email { get; }

    /// <summary>Throws when no customer token is present.</summary>
    long RequireCustomerId();
}
