using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Interfaces;

namespace ChefLan.Infrastructure.Repositories.Courses;

/// <summary>
/// SKELETON — not implemented yet. Arrives with the public category endpoint and admin
/// course editing (step 2). GetByIdsAsync exists so assigning categories to a course is
/// one round trip rather than N.
/// </summary>
internal sealed class CategoryRepository(CoursesDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CategoryRepository.GetByIdAsync — step 2.");

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CategoryRepository.GetBySlugAsync — step 2.");

    public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CategoryRepository.ListAsync — step 2 (GET /api/v1/categories).");

    public Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CategoryRepository.GetByIdsAsync — step 2.");

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("CategoryRepository.SlugExistsAsync — step 2.");

    public void Add(Category category) => context.Categories.Add(category);

    public void Remove(Category category) => context.Categories.Remove(category);
}
