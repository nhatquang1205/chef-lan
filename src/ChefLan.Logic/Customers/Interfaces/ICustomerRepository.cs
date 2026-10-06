using ChefLan.Commons.Pagination;
using ChefLan.Domain.Customers.Entities;

namespace ChefLan.Logic.Customers.Interfaces;

/// <summary>Port for external CUSTOMER accounts.</summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Only matches customers that have an email on record.</summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<PagedResult<Customer>> ListAsync(PagedRequest request, string? search, CancellationToken cancellationToken = default);

    void Add(Customer customer);
}
