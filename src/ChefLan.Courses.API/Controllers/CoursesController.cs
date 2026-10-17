using ChefLan.Commons.Auth;
using ChefLan.Commons.Pagination;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

/// <summary>
/// The catalog. The course list and course page are public (the storefront); a lesson page needs a signed-in
/// customer or staff account.
///
/// Note what is injected — ICourseCatalogService, from ChefLan.Logic. The DbContext is
/// not reachable from here, which is the point of the layering.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class CoursesController(ICourseCatalogService catalog, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Lists published courses, newest first by default.</summary>
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

        return Ok(await catalog.ListPublishedAsync(query, cancellationToken));
    }

    /// <summary>
    /// The course page: outline plus the videos a visitor may watch. A draft is a 404 here.
    /// Non-free videos carry no URL.
    /// </summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CourseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDetailResponse>> Get(string slug, CancellationToken cancellationToken)
    {
        long? customerId = currentUser.TryGetCustomerId(out var id) ? id : null;

        return (await catalog.GetPublishedBySlugAsync(slug, customerId, cancellationToken)).ToActionResult(this);
    }

    /// <summary>
    /// The lesson page: outline, video list and, for a customer who owns the lesson, the recipe.
    /// The answer depends on who asks, so it is never cached.
    /// </summary>
    [HttpGet("{slug}/lessons/{lessonSlug}")]
    [Authorize(Policy = ChefLanPolicies.AnyUser)]
    [ProducesResponseType(typeof(LessonPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LessonPageResponse>> GetLesson(string slug, string lessonSlug, CancellationToken cancellationToken)
    {
        long? customerId = currentUser.TryGetCustomerId(out var id) ? id : null;

        Response.Headers.CacheControl = "private, no-store";

        return (await catalog.GetPublishedLessonAsync(slug, lessonSlug, customerId, currentUser.TryGetStaffId(out _), cancellationToken)).ToActionResult(this);
    }
}
