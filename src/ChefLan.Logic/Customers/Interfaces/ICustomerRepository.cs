using ChefLan.Commons.Pagination;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Contracts;

namespace ChefLan.Logic.Customers.Interfaces;

/// <summary>Port for external CUSTOMER accounts.</summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Only matches customers that have an email on record.</summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Search covers name, email, phone and Facebook name; newest first.</summary>
    Task<PagedResult<Customer>> ListAsync(
        PagedRequest request,
        string? search,
        CustomerStatus? status,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked. The customer whose pending invitation has this token hash (expired or not).</summary>
    Task<Customer?> GetByInviteTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    void Add(Customer customer);
}
