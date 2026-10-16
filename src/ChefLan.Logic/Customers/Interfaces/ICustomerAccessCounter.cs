using ChefLan.Logic.Customers.Contracts;

namespace ChefLan.Logic.Customers.Interfaces;

/// <summary>
/// The Customers domain's read port onto what customers hold. Implemented in Infrastructure, which
/// can see the courses tables, so Logic.Customers never references Logic.Courses.
/// </summary>
public interface ICustomerAccessCounter
{
    Task<IReadOnlyDictionary<long, CustomerAccessCounts>> CountActiveAsync(
        IReadOnlyCollection<long> customerIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
