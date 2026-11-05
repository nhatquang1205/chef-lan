using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class OrderRepository(CoursesDbContext context) : IOrderRepository
{
    public Task<Order?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Code == code, cancellationToken);

    public Task<Order?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Orders
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> FindPendingForItemAsync(
        long customerId,
        OrderItemKind kind,
        long targetId,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        FindPendingAsync(customerId, kind, kind == OrderItemKind.Course ? targetId : null, kind == OrderItemKind.Lesson ? targetId : null, utcNow, cancellationToken);

    private Task<Order?> FindPendingAsync(
        long customerId,
        OrderItemKind kind,
        long? courseId,
        long? lessonId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId
                && o.Status == OrderStatus.Pending
                && o.ExpiresAt > utcNow
                && o.Items.Any(i => i.Kind == kind
                    && ((courseId != null && i.CourseId == courseId) || (lessonId != null && i.LessonId == lessonId))))
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        context.Orders.AnyAsync(o => o.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListForCustomerAsync(long customerId, int take, CancellationToken cancellationToken = default) =>
        await context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Order>> ListAsync(
        AdminOrderFilter filter,
        DateTime utcNow,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = context.Orders.AsNoTracking();

        // Mirrors Order.EffectiveStatus.
        query = filter.Status switch
        {
            OrderStatus.Pending => query.Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt > utcNow),
            OrderStatus.Expired => query.Where(o => o.Status == OrderStatus.Expired
                || (o.Status == OrderStatus.Pending && o.ExpiresAt <= utcNow)),
            { } stored => query.Where(o => o.Status == stored),
            null => query,
        };

        if (filter.PaymentMethod is { } method)
        {
            query = query.Where(o => o.PaymentMethod == method);
        }

        if (filter.CustomerId is { } customerId)
        {
            query = query.Where(o => o.CustomerId == customerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var term = filter.Q.Trim();
            var pattern = LikePattern.Contains(term);
            query = query.Where(o =>
                EF.Functions.ILike(o.Code, pattern, LikePattern.EscapeCharacter)
                || (o.Customer != null
                    && (EF.Functions.ILike(o.Customer.FullName, pattern, LikePattern.EscapeCharacter)
                        || (o.Customer.Email != null && EF.Functions.ILike(o.Customer.Email, pattern, LikePattern.EscapeCharacter))
                        || (o.Customer.Phone != null && EF.Functions.ILike(o.Customer.Phone, pattern, LikePattern.EscapeCharacter)))));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Order>.Create(items, paging.Page, paging.PageSize, total);
    }

    public void Add(Order order) => context.Orders.Add(order);
}
