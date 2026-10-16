using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Logic.Customers.Contracts;

namespace ChefLan.Logic.Customers.Interfaces;

public interface ICustomerAdminService
{
    Task<PagedResult<CustomerAdminListItem>> ListAsync(CustomerAdminQuery query, CancellationToken cancellationToken = default);

    Task<Result<CustomerAdminDetail>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<CustomerAdminDetail>> CreateAsync(
        CustomerAdminRequest request, long? staffId, CancellationToken cancellationToken = default);

    Task<Result<CustomerAdminDetail>> UpdateAsync(
        long id, CustomerAdminRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends (or resends) the invitation mail. A new link replaces the previous one; it is valid for 7 days.
    /// </summary>
    Task<Result<CustomerInviteResponse>> InviteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Idempotent. Also revokes every refresh token the customer holds.</summary>
    Task<Result> DeactivateAsync(long id, CancellationToken cancellationToken = default);

    Task<Result> ReactivateAsync(long id, CancellationToken cancellationToken = default);
}
