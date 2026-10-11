using ChefLan.Commons.Pagination;
using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Contracts.Requests;

namespace ChefLan.Logic.Courses.Interfaces;

public interface ICourseRepository
{
    /// <summary>Published, non-deleted courses only — the public catalog.</summary>
    Task<PagedResult<Course>> ListPublishedAsync(
        CourseListQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Drafts included.</summary>
    Task<PagedResult<Course>> ListAsync(
        AdminCourseListQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The public detail: categories, published lessons and their published videos, in order.
    /// Read-only. A draft is not reachable by slug.
    /// </summary>
    Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Tracked, no navigations.</summary>
    Task<Course?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Tracked, with its category links and the categories themselves.</summary>
    Task<Course?> GetByIdWithCategoriesAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetSlugsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    void Add(Course course);

    void Remove(Course course);
}
