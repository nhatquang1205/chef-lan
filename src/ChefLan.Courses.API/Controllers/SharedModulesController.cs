using ChefLan.Commons.Pagination;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

/// <summary>The published shared modules, kept off the course list. A module's own page is still <c>/courses/{slug}</c>.</summary>
[ApiController]
[Route("api/v1/shared-modules")]
[Produces("application/json")]
public sealed class SharedModulesController(ICourseCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
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

        return Ok(await catalog.ListSharedModulesAsync(query, cancellationToken));
    }
}
