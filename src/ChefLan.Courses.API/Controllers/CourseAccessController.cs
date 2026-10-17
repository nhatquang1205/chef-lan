using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[Authorize(Policy = ChefLanPolicies.AnyUser)]
[Route("api/v1/courses/{slug}/access")]
[Produces("application/json")]
public sealed class CourseAccessController(IEnrollmentService enrollments, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// What the signed-in customer owns in this course (a staff account: everything published): the whole course, and/or individual lessons.
    /// The course page uses it to unlock the outline; the playback endpoint is what actually enforces it.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CourseAccessResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseAccessResponse>> Get(string slug, CancellationToken cancellationToken)
    {
        if (currentUser.TryGetStaffId(out _))
        {
            return (await enrollments.GetStaffCourseAccessAsync(slug, cancellationToken)).ToActionResult(this);
        }

        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        return (await enrollments.GetCourseAccessAsync(slug, customerId, cancellationToken)).ToActionResult(this);
    }
}
