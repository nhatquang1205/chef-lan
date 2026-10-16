using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Commons.Time;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Abstractions;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Errors;
using ChefLan.Logic.Customers.Interfaces;
using ChefLan.Logic.Customers.Mapping;

namespace ChefLan.Logic.Customers.Services;

internal sealed class CustomerAdminService(
    ICustomerRepository customers,
    ICustomerRefreshTokenRepository refreshTokens,
    ICustomerAccessCounter accessCounter,
    IUnitOfWork unitOfWork,
    IClock clock) : ICustomerAdminService
{
    public async Task<PagedResult<CustomerAdminListItem>> ListAsync(
        CustomerAdminQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await customers.ListAsync(
            new PagedRequest { Page = query.Page, PageSize = query.PageSize },
            Clean(query.Q),
            query.Status,
            cancellationToken);

        var counts = await accessCounter.CountActiveAsync(
            page.Items.Select(c => c.Id).ToList(), clock.UtcNow, cancellationToken);

        var items = page.Items
            .Select(c => c.ToListItem(counts.GetValueOrDefault(c.Id)))
            .ToList();

        return PagedResult<CustomerAdminListItem>.Create(items, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<Result<CustomerAdminDetail>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        return customer is null ? CustomerErrors.NotFound(id) : customer.ToAdminDetail();
    }

    public async Task<Result<CustomerAdminDetail>> CreateAsync(
        CustomerAdminRequest request,
        long? staffId,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (email is not null && await customers.EmailExistsAsync(email, cancellationToken))
        {
            return CustomerErrors.EmailAlreadyUsed(email);
        }

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = Clean(request.Phone),
            FacebookName = Clean(request.FacebookName),
            FacebookProfileUrl = Clean(request.FacebookProfileUrl),
            Note = Clean(request.Note),
            CreatedByStaffId = staffId,
        };

        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToAdminDetail();
    }

    public async Task<Result<CustomerAdminDetail>> UpdateAsync(
        long id,
        CustomerAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return CustomerErrors.NotFound(id);
        }

        var email = NormalizeEmail(request.Email);

        if (!string.Equals(email, customer.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (email is not null)
            {
                var owner = await customers.GetByEmailAsync(email, cancellationToken);

                if (owner is not null && owner.Id != customer.Id)
                {
                    return CustomerErrors.EmailAlreadyUsed(email);
                }
            }

            customer.Email = email;
        }

        customer.FullName = request.FullName.Trim();
        customer.Phone = Clean(request.Phone);
        customer.FacebookName = Clean(request.FacebookName);
        customer.FacebookProfileUrl = Clean(request.FacebookProfileUrl);
        customer.Note = Clean(request.Note);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToAdminDetail();
    }

    public async Task<Result> DeactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(CustomerErrors.NotFound(id));
        }

        customer.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await refreshTokens.RevokeAllForCustomerAsync(customer.Id, clock.UtcNow, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReactivateAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(CustomerErrors.NotFound(id));
        }

        customer.IsActive = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
