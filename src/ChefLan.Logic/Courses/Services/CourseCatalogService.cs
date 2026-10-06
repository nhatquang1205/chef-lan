using ChefLan.Commons.Pagination;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using ChefLan.Logic.Courses.Mapping;

namespace ChefLan.Logic.Courses.Services;

/// <summary>
/// internal sealed: the API cannot resolve this by name, only through
/// <see cref="ICourseCatalogService"/> registered by <see cref="CoursesModule"/>.
/// </summary>
internal sealed class CourseCatalogService(ICourseRepository courses) : ICourseCatalogService
{
    public async Task<PagedResult<CourseListItemResponse>> ListPublishedAsync(
        CourseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await courses.ListPublishedAsync(query, cancellationToken);

        return PagedResult<CourseListItemResponse>.Create(
            page.Items.Select(c => c.ToListItemResponse()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }
}
