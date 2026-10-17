using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

/// <summary>
/// The course gallery. Files are uploaded first through the uploads endpoint; these calls only attach the
/// returned URL, so they are small JSON requests.
/// </summary>
[ApiController]
[Route("api/v1/admins")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminCourseImagesController(ICourseImageService images) : ControllerBase
{
    private const string ListRouteName = "AdminListCourseImages";

    [HttpGet("courses/{courseId:long}/images", Name = ListRouteName)]
    [ProducesResponseType(typeof(IReadOnlyList<CourseImageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseImageResponse>>> List(
        long courseId,
        CancellationToken cancellationToken) =>
        (await images.ListAsync(courseId, cancellationToken)).ToActionResult(this);

    [HttpPost("courses/{courseId:long}/images")]
    [ProducesResponseType(typeof(CourseImageResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseImageResponse>> Create(
        long courseId,
        CourseImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await images.CreateAsync(courseId, request, cancellationToken);

        return result.ToCreatedResult(this, ListRouteName, new { courseId });
    }

    [HttpPut("courses/{courseId:long}/images/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Reorder(
        long courseId,
        CourseImageOrderRequest request,
        CancellationToken cancellationToken) =>
        (await images.ReorderAsync(courseId, request, cancellationToken)).ToActionResult(this);

    [HttpPut("course-images/{imageId:long}")]
    public async Task<ActionResult<CourseImageResponse>> Update(
        long imageId,
        CourseImageUpdateRequest request,
        CancellationToken cancellationToken) =>
        (await images.UpdateAsync(imageId, request, cancellationToken)).ToActionResult(this);

    [HttpDelete("course-images/{imageId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(long imageId, CancellationToken cancellationToken) =>
        (await images.DeleteAsync(imageId, cancellationToken)).ToActionResult(this);
}
