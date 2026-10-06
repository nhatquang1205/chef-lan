using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Interfaces;

namespace ChefLan.Infrastructure.Repositories.Customers;

/// <summary>
/// SKELETON — not implemented yet. Arrives with the customer auth endpoints (step 2).
///
/// When implementing, two things specific to this table:
///  - email is NULLABLE, so GetByEmailAsync must match only rows that have one. A
///    credential-less Facebook-era customer has no email and must never match.
///  - ListAsync's search should cover full_name, phone and facebook_name, not just email,
///    because that is how staff actually look people up. Those indexes exist for it.
/// </summary>
internal sealed class CustomerRepository(CoursesDbContext context) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CustomerRepository.GetByIdAsync — step 2 (customer auth).");

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CustomerRepository.GetByEmailAsync — step 2 (customer auth).");

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CustomerRepository.EmailExistsAsync — step 2 (registration).");

    public Task<PagedResult<Customer>> ListAsync(
        PagedRequest request,
        string? search,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CustomerRepository.ListAsync — step 2 (admin customer list).");

    public void Add(Customer customer) => context.Customers.Add(customer);
}
