using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ICategoryRepository
{
    /// <summary>Tracked, because the update and delete use cases modify it.</summary>
    Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Ordered by display_order, then name. Both filters are optional.</summary>
    Task<IReadOnlyList<Category>> ListAsync(
        CategoryStatus? status,
        string? search,
        CancellationToken cancellationToken = default);

    /// <summary>Live categories only — a soft-deleted id is simply absent from the result.</summary>
    Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);

    /// <summary>Slugs of live categories that start with <paramref name="prefix"/>.</summary>
    Task<IReadOnlySet<string>> GetSlugsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>Is any non-deleted course attached to the category?</summary>
    Task<bool> IsInUseAsync(long id, CancellationToken cancellationToken = default);

    void Add(Category category);

    void Remove(Category category);
}
