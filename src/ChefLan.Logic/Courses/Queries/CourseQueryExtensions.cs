using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;

namespace ChefLan.Logic.Courses.Queries;

/// <summary>
/// What "published", "in this category" and "newest first" MEAN for a course. These are
/// business rules, so they live in the application layer, not in a repository.
///
/// This is pure LINQ over <see cref="IQueryable{T}"/>, which is BCL (System.Linq), not
/// EF Core — so declaring them here keeps ChefLan.Logic free of any ORM dependency. The
/// expressions are translated to SQL by whichever provider the repository is built on.
///
/// Provider-specific predicates do NOT belong here. Case-insensitive search needs
/// Postgres ILIKE, so it is applied inline in CourseRepository instead.
/// </summary>
public static class CourseQueryExtensions
{
    public static IQueryable<Course> OnlyPublished(this IQueryable<Course> query) =>
        query.Where(c => c.Status == CourseStatus.Published);

    public static IQueryable<Course> WithStatus(this IQueryable<Course> query, CourseStatus? status) =>
        status is null ? query : query.Where(c => c.Status == status);

    public static IQueryable<Course> WithCategorySlug(this IQueryable<Course> query, string? categorySlug) =>
        string.IsNullOrWhiteSpace(categorySlug)
            ? query
            : query.Where(c => c.CourseCategories.Any(cc => cc.Category!.Slug == categorySlug));

    public static IQueryable<Course> WithLevel(this IQueryable<Course> query, CourseLevel? level) =>
        level is null ? query : query.Where(c => c.Level == level);

    /// <summary>
    /// Every branch adds a tiebreaker on Id. Without it, paging over rows with equal sort
    /// keys is not deterministic and a course can appear on two pages or neither.
    /// </summary>
    public static IQueryable<Course> SortBy(this IQueryable<Course> query, CourseSortOrder sort) => sort switch
    {
        CourseSortOrder.PriceAsc => query.OrderBy(c => c.PriceVnd).ThenBy(c => c.Id),
        CourseSortOrder.PriceDesc => query.OrderByDescending(c => c.PriceVnd).ThenBy(c => c.Id),
        CourseSortOrder.Title => query.OrderBy(c => c.Title).ThenBy(c => c.Id),
        _ => query.OrderByDescending(c => c.PublishedAt).ThenByDescending(c => c.Id),
    };
}
