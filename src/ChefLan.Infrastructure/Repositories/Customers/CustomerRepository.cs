using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Contracts;
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

    public async Task<PagedResult<Customer>> ListAsync(
        PagedRequest request,
        string? search,
        CustomerStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(c =>
                EF.Functions.ILike(c.FullName, pattern, LikePattern.EscapeCharacter)
                || (c.Email != null && EF.Functions.ILike(c.Email, pattern, LikePattern.EscapeCharacter))
                || (c.Phone != null && EF.Functions.ILike(c.Phone, pattern, LikePattern.EscapeCharacter))
                || (c.FacebookName != null && EF.Functions.ILike(c.FacebookName, pattern, LikePattern.EscapeCharacter)));
        }

        // Mirrors CustomerMappings.StatusOf. InviteExpired has no data until invitations add expiry columns.
        query = status switch
        {
            CustomerStatus.Inactive => query.Where(c => !c.IsActive),
            CustomerStatus.Active => query.Where(c => c.IsActive && (c.PasswordHash != null || c.ActivatedAt != null)),
            CustomerStatus.Invited => query.Where(c => c.IsActive && c.PasswordHash == null && c.ActivatedAt == null && c.InvitedAt != null),
            CustomerStatus.NotInvited => query.Where(c => c.IsActive && c.PasswordHash == null && c.ActivatedAt == null && c.InvitedAt == null),
            CustomerStatus.InviteExpired => query.Where(_ => false),
            _ => query,
        };

        var total = await query.CountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<Customer>.Empty(request.Page, request.PageSize);
        }

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Customer>.Create(items, request.Page, request.PageSize, total);
    }

    public void Add(Customer customer) => context.Customers.Add(customer);
}
