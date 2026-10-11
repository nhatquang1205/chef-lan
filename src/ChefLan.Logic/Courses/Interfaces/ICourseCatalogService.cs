using ChefLan.Commons.Pagination;
using ChefLan.Commons.Results;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;

namespace ChefLan.Logic.Courses.Interfaces;

/// <summary>The public, unauthenticated course catalog.</summary>
public interface ICourseCatalogService
{
    Task<PagedResult<CourseListItemResponse>> ListPublishedAsync(
        CourseListQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="customerId"/> is the signed-in customer if there is one. It does not
    /// change what is returned yet — enrollment gating arrives with the enrollment endpoints.
    /// </summary>
    Task<Result<CourseDetailResponse>> GetPublishedBySlugAsync(
        string slug,
        long? customerId,
        CancellationToken cancellationToken = default);
}
