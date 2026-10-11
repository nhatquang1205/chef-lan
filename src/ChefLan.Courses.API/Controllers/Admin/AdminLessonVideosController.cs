using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

/// <summary>
/// There is no upload endpoint yet: videos go up through the Bunny dashboard and the video
/// GUID is pasted into <c>externalId</c>.
/// </summary>
[ApiController]
[Route("api/v1/admins")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminLessonVideosController(ILessonVideoService videos) : ControllerBase
{
    private const string GetRouteName = "AdminGetLessonVideo";

    [HttpGet("lessons/{lessonId:long}/videos")]
    [ProducesResponseType(typeof(IReadOnlyList<LessonVideoAdminResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LessonVideoAdminResponse>>> List(
        long lessonId,
        CancellationToken cancellationToken) =>
        (await videos.ListAsync(lessonId, cancellationToken)).ToActionResult(this);

    [HttpPost("lessons/{lessonId:long}/videos")]
    [ProducesResponseType(typeof(LessonVideoAdminResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LessonVideoAdminResponse>> Create(
        long lessonId,
        LessonVideoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await videos.CreateAsync(lessonId, request, cancellationToken);

        return result.ToCreatedResult(this, GetRouteName, new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    [HttpPut("lessons/{lessonId:long}/videos/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Reorder(
        long lessonId,
        LessonVideoOrderRequest request,
        CancellationToken cancellationToken) =>
        (await videos.ReorderAsync(lessonId, request, cancellationToken)).ToActionResult(this);

    [HttpGet("lesson-videos/{id:long}", Name = GetRouteName)]
    public async Task<ActionResult<LessonVideoAdminResponse>> Get(long id, CancellationToken cancellationToken) =>
        (await videos.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPut("lesson-videos/{id:long}")]
    public async Task<ActionResult<LessonVideoAdminResponse>> Update(
        long id,
        LessonVideoRequest request,
        CancellationToken cancellationToken) =>
        (await videos.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    [HttpDelete("lesson-videos/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken) =>
        (await videos.DeleteAsync(id, cancellationToken)).ToActionResult(this);
}
