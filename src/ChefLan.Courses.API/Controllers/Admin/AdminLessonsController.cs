using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

/// <summary>Create and list are nested under the course; item operations are flat.</summary>
[ApiController]
[Route("api/v1/admins")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminLessonsController(ILessonService lessons) : ControllerBase
{
    private const string GetRouteName = "AdminGetLesson";

    [HttpGet("courses/{courseId:long}/lessons")]
    [ProducesResponseType(typeof(IReadOnlyList<LessonAdminListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LessonAdminListItemResponse>>> List(
        long courseId,
        CancellationToken cancellationToken) =>
        (await lessons.ListAsync(courseId, cancellationToken)).ToActionResult(this);

    [HttpPost("courses/{courseId:long}/lessons")]
    [ProducesResponseType(typeof(LessonAdminDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LessonAdminDetailResponse>> Create(
        long courseId,
        LessonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lessons.CreateAsync(courseId, request, cancellationToken);

        return result.ToCreatedResult(this, GetRouteName, new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    /// <summary>Takes the full ordered list of the course's lesson ids; rewrites the order in one transaction.</summary>
    [HttpPut("courses/{courseId:long}/lessons/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Reorder(
        long courseId,
        LessonOrderRequest request,
        CancellationToken cancellationToken) =>
        (await lessons.ReorderAsync(courseId, request, cancellationToken)).ToActionResult(this);

    [HttpGet("lessons/{id:long}", Name = GetRouteName)]
    public async Task<ActionResult<LessonAdminDetailResponse>> Get(long id, CancellationToken cancellationToken) =>
        (await lessons.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPut("lessons/{id:long}")]
    public async Task<ActionResult<LessonAdminDetailResponse>> Update(
        long id,
        LessonRequest request,
        CancellationToken cancellationToken) =>
        (await lessons.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    [HttpDelete("lessons/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken) =>
        (await lessons.DeleteAsync(id, cancellationToken)).ToActionResult(this);
}
