using ChefLan.Commons.Pagination;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

/// <summary>The public, unauthenticated course catalog.</summary>
public interface ICourseCatalogService
{
    Task<PagedResult<CourseListItemResponse>> ListPublishedAsync(
        CourseListQuery query,
        CancellationToken cancellationToken = default);
}
