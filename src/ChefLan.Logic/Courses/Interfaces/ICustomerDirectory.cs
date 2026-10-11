namespace ChefLan.Logic.Courses.Interfaces;

public sealed record CustomerRef(long Id, string? Email, string FullName, bool IsActive);

/// <summary>
/// The Courses domain's own view of "who is this customer", so granting access never reaches into the
/// Customers domain (no cross-domain references). Implemented over the same database by Infrastructure.
/// </summary>
public interface ICustomerDirectory
{
    /// <summary>Finds a live customer by id or by email (exactly one is given). Null when there is none.</summary>
    Task<CustomerRef?> FindAsync(long? customerId, string? email, CancellationToken cancellationToken = default);
}
