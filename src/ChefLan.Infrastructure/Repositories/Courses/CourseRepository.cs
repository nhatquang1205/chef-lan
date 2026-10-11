using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Queries;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class CourseRepository(CoursesDbContext context) : ICourseRepository
{
    public Task<PagedResult<Course>> ListPublishedAsync(
        CourseListQuery query,
        CancellationToken cancellationToken = default) =>
        PageAsync(context.Courses.AsNoTracking().OnlyPublished(), query, cancellationToken);

    public Task<PagedResult<Course>> ListAsync(
        AdminCourseListQuery query,
        CancellationToken cancellationToken = default) =>
        PageAsync(context.Courses.AsNoTracking().WithStatus(query.Status), query, cancellationToken);

    private static async Task<PagedResult<Course>> PageAsync(
        IQueryable<Course> source,
        CourseListQuery query,
        CancellationToken cancellationToken)
    {
        var filtered = source
            .WithCategorySlug(query.CategorySlug)
            .WithLevel(query.Level);

        // Inlined rather than an extension method, because EF.Functions.ILike is
        // Npgsql-specific: as an extension it would either sit in ChefLan.Logic and drag
        // the ORM into the application layer, or form a second, near-empty extension class
        // here. The provider-agnostic predicates come from ChefLan.Logic.
        //
        // ILIKE is accent-SENSITIVE: "pho" does not match "Phở", and Vietnamese users
        // routinely type without accents. The fix is searching the already-unaccented slug
        // column, or unaccent + a GIN index — Postgres has no Vietnamese dictionary.
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q.Trim());

            filtered = filtered.Where(c =>
                EF.Functions.ILike(c.Title, pattern, LikePattern.EscapeCharacter) ||
                (c.Subtitle != null && EF.Functions.ILike(c.Subtitle, pattern, LikePattern.EscapeCharacter)));
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<Course>.Empty(query.Page, query.PageSize);
        }

        var items = await filtered
            .SortBy(query.Sort)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
            .ToListAsync(cancellationToken);

        return PagedResult<Course>.Create(items, query.Page, query.PageSize, totalCount);
    }

    /// <summary>
    /// Filtered includes inherit the soft-delete filter, so deleted lessons and videos never
    /// appear. Unpublished ones are excluded explicitly.
    /// </summary>
    public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        context.Courses
            .AsNoTracking()
            .AsSplitQuery()
            .OnlyPublished()
            .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
            .Include(c => c.Lessons.Where(l => l.IsPublished).OrderBy(l => l.SortOrder).ThenBy(l => l.Id))
                .ThenInclude(l => l.Videos.Where(v => v.IsPublished).OrderBy(v => v.SortOrder).ThenBy(v => v.Id))
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    public Task<long?> GetPublishedIdBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        context.Courses
            .AsNoTracking()
            .OnlyPublished()
            .Where(c => c.Slug == slug)
            .Select(c => (long?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Course?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Course?> GetByIdWithCategoriesAsync(long id, CancellationToken cancellationToken = default) =>
        context.Courses
            .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <summary>
    /// Honours the soft-delete query filter on purpose: uq_courses_slug is partial on
    /// is_deleted = false, so a deleted course releases its slug for reuse.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetSlugsWithPrefixAsync(
        string prefix,
        CancellationToken cancellationToken = default) =>
        (await context.Courses
            .Where(c => c.Slug.StartsWith(prefix))
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken)).ToHashSet();

    public void Add(Course course) => context.Courses.Add(course);

    /// <summary>Soft delete — the interceptor turns this into an update.</summary>
    public void Remove(Course course) => context.Courses.Remove(course);
}
