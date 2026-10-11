using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Customers;

/// <summary>
/// email is NULLABLE, so lookups match only rows that have one — a credential-less
/// Facebook-era customer has no email and never matches. The soft-delete query filter
/// mirrors uq_customers_email, so a deleted customer's email is free to register again.
/// </summary>
internal sealed class CustomerRepository(CoursesDbContext context) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Customers.FirstOrDefaultAsync(c => c.Email != null && c.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        context.Customers.AnyAsync(c => c.Email != null && c.Email == email, cancellationToken);

    /// <summary>
    /// SKELETON — arrives with the admin customer endpoints. Search should cover full_name,
    /// phone and facebook_name, because that is how staff look people up.
    /// </summary>
    public Task<PagedResult<Customer>> ListAsync(
        PagedRequest request,
        string? search,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CustomerRepository.ListAsync — admin customer list.");

    public void Add(Customer customer) => context.Customers.Add(customer);
}
