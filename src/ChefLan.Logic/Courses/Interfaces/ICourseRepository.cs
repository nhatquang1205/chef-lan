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

    Task<Course?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}
