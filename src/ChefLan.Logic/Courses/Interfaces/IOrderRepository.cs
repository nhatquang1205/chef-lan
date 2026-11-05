using ChefLan.Commons.Pagination;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;

namespace ChefLan.Logic.Courses.Interfaces;

public interface IOrderRepository
{
    /// <summary>Tracked, with items and customer.</summary>
    Task<Order?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Tracked, with items and customer.</summary>
    Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Tracked. The customer's still-payable order for the same item, if there is one.</summary>
    Task<Order?> FindPendingForItemAsync(
        long customerId,
        OrderItemKind kind,
        long targetId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Read-only, with items. Newest first.</summary>
    Task<IReadOnlyList<Order>> ListForCustomerAsync(long customerId, int take, CancellationToken cancellationToken = default);

    /// <summary>Read-only, with items and customer. Newest first. A status filter is on the EFFECTIVE status.</summary>
    Task<PagedResult<Order>> ListAsync(AdminOrderFilter filter, DateTime utcNow, PagedRequest paging, CancellationToken cancellationToken = default);

    void Add(Order order);
}
