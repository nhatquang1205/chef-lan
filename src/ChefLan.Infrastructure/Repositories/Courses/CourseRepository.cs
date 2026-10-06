using ChefLan.Commons.Pagination;
using ChefLan.DB.Contexts;
using ChefLan.Logic.Courses.Queries;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChefLan.Infrastructure.Repositories.Courses;

internal sealed class CourseRepository(CoursesDbContext context) : ICourseRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<Course>> ListPublishedAsync(
        CourseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var filtered = context.Courses
            .AsNoTracking()
            .OnlyPublished()
            .WithCategorySlug(query.CategorySlug)
            .WithLevel(query.Level);

        // Inlined rather than an extension method, because EF.Functions.ILike is
        // Npgsql-specific: as an extension it would either sit in ChefLan.Logic and drag
        // the ORM into the application layer, or form a second, near-empty extension class
        // here. The provider-agnostic predicates above come from ChefLan.Logic.
        //
        // ILIKE is accent-SENSITIVE: "pho" does not match "Phở", and Vietnamese users
        // routinely type without accents. The fix is searching the already-unaccented slug
        // column, or unaccent + a GIN index — Postgres has no Vietnamese dictionary.
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = $"%{EscapeLikePattern(query.Q.Trim())}%";

            filtered = filtered.Where(c =>
                EF.Functions.ILike(c.Title, pattern, LikeEscapeCharacter) ||
                (c.Subtitle != null && EF.Functions.ILike(c.Subtitle, pattern, LikeEscapeCharacter)));
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

    public Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        context.Courses
            .AsNoTracking()
            .OnlyPublished()
            .Include(c => c.CourseCategories)
                .ThenInclude(cc => cc.Category)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

    /// <summary>
    /// Escapes the LIKE metacharacters so a search for "%" or "_" matches those literal
    /// characters instead of acting as a wildcard. Not an injection risk — EF parameterises
    /// the pattern — but without this, searching "%" returns the entire catalog.
    /// The backslash must be escaped first, or it would double-escape the others.
    /// </summary>
    private static string EscapeLikePattern(string input) => input
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");

    /// <summary>
    /// Honours the soft-delete query filter on purpose: uq_courses_slug is partial on
    /// is_deleted = false, so a deleted course releases its slug for reuse.
    /// </summary>
    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        context.Courses.AnyAsync(c => c.Slug == slug, cancellationToken);
}
