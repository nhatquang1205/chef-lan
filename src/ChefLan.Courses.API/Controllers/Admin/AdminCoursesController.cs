using ChefLan.Commons.Auth;
using ChefLan.Commons.Pagination;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admins/courses")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminCoursesController(ICourseService courses) : ControllerBase
{
    private const string GetRouteName = "AdminGetCourse";

    /// <summary>Drafts included, unlike the public list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CourseListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseListItemResponse>>> List(
        [FromQuery] CourseStatus? status,
        [FromQuery] string? q,
        [FromQuery] string? categorySlug,
        [FromQuery] CourseLevel? level,
        [FromQuery] CourseSortOrder sort = CourseSortOrder.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var query = new AdminCourseListQuery
        {
            Status = status,
            Q = q,
            CategorySlug = categorySlug,
            Level = level,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
        };

        return Ok(await courses.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}", Name = GetRouteName)]
    public async Task<ActionResult<CourseAdminDetailResponse>> Get(long id, CancellationToken cancellationToken) =>
        (await courses.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [ProducesResponseType(typeof(CourseAdminDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseAdminDetailResponse>> Create(
        CourseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courses.CreateAsync(request, cancellationToken);

        return result.ToCreatedResult(this, GetRouteName, new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    /// <summary>Status is not a field here — use publish and unpublish.</summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<CourseAdminDetailResponse>> Update(
        long id,
        CourseRequest request,
        CancellationToken cancellationToken) =>
        (await courses.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken) =>
        (await courses.DeleteAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>409 unless the course has a published lesson holding a playable video.</summary>
    [HttpPost("{id:long}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Publish(long id, CancellationToken cancellationToken) =>
        (await courses.PublishAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:long}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Unpublish(long id, CancellationToken cancellationToken) =>
        (await courses.UnpublishAsync(id, cancellationToken)).ToActionResult(this);
}
