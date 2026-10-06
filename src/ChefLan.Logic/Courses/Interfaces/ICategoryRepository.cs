using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Ordered by display_order.</summary>
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    void Add(Category category);

    void Remove(Category category);
}
