using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admins/courses/{courseId:long}")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminCourseIncludesController(ICourseIncludeService includes) : ControllerBase
{
    [HttpGet("includes")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseIncludeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseIncludeResponse>>> List(
        long courseId,
        CancellationToken cancellationToken) =>
        (await includes.ListAsync(courseId, cancellationToken)).ToActionResult(this);

    /// <summary>Adds the pair (at the end of the order) or updates its discount.</summary>
    [HttpPut("includes/{includedCourseId:long}")]
    public async Task<ActionResult<CourseIncludeResponse>> Upsert(
        long courseId,
        long includedCourseId,
        CourseIncludeRequest request,
        CancellationToken cancellationToken) =>
        (await includes.UpsertAsync(courseId, includedCourseId, request, cancellationToken)).ToActionResult(this);

    [HttpDelete("includes/{includedCourseId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Remove(
        long courseId,
        long includedCourseId,
        CancellationToken cancellationToken) =>
        (await includes.RemoveAsync(courseId, includedCourseId, cancellationToken)).ToActionResult(this);

    [HttpPut("includes/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Reorder(
        long courseId,
        CourseIncludeOrderRequest request,
        CancellationToken cancellationToken) =>
        (await includes.ReorderAsync(courseId, request, cancellationToken)).ToActionResult(this);

    [HttpGet("included-in")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseIncludedInResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseIncludedInResponse>>> IncludedIn(
        long courseId,
        CancellationToken cancellationToken) =>
        (await includes.ListIncludedInAsync(courseId, cancellationToken)).ToActionResult(this);
}
