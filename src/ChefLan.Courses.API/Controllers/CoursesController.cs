using ChefLan.Commons.Pagination;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

/// <summary>
/// The public catalog. Anonymous: this is the storefront.
///
/// Note what is injected — ICourseCatalogService, from ChefLan.Logic. The DbContext is
/// not reachable from here, which is the point of the layering.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class CoursesController(ICourseCatalogService catalog) : ControllerBase
{
    /// <summary>Lists published courses, newest first by default.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CourseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseListItemResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? q = null,
        [FromQuery] string? categorySlug = null,
        [FromQuery] CourseLevel? level = null,
        [FromQuery] CourseSortOrder sort = CourseSortOrder.Newest,
        CancellationToken cancellationToken = default)
    {
        var query = new CourseListQuery
        {
            Page = page,
            PageSize = pageSize,
            Q = q,
            CategorySlug = categorySlug,
            Level = level,
            Sort = sort,
        };

        return Ok(await catalog.ListPublishedAsync(query, cancellationToken));
    }
}
