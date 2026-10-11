using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class CategoryRepository(CoursesDbContext context) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(
        CategoryStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = context.Categories.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePattern.Contains(search.Trim());
            query = query.Where(c => EF.Functions.ILike(c.Name, pattern, LikePattern.EscapeCharacter));
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken = default) =>
        await context.Categories.Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);

    /// <summary>
    /// Honours the soft-delete filter on purpose: uq_categories_slug is partial on
    /// is_deleted = false, so a deleted category releases its slug.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetSlugsWithPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default) =>
        (await context.Categories
            .Where(c => c.Slug.StartsWith(prefix))
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken)).ToHashSet();

    public Task<bool> IsInUseAsync(long id, CancellationToken cancellationToken = default) =>
        context.CourseCategories.AnyAsync(
            cc => cc.CategoryId == id && !cc.Course!.IsDeleted,
            cancellationToken);

    public void Add(Category category) => context.Categories.Add(category);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    public void Remove(Category category) => context.Categories.Remove(category);
}
