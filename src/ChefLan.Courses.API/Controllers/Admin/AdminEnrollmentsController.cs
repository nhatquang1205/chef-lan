using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

/// <summary>
/// Staff grant access by hand (there is no checkout yet): to a whole course, which includes every
/// lesson, or to a single lesson. Identify the customer by <c>customerId</c> or <c>customerEmail</c>.
/// </summary>
[ApiController]
[Route("api/v1/admins")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminEnrollmentsController(IEnrollmentService enrollments, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Grants access to a whole course. 409 if the customer already has active access.</summary>
    [HttpPost("enrollments")]
    [ProducesResponseType(typeof(CourseEnrollmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CourseEnrollmentResponse>> GrantCourse(
        GrantCourseAccessRequest request,
        CancellationToken cancellationToken)
    {
        var result = await enrollments.GrantCourseAsync(request, StaffId(), cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : result.ToActionResult(this);
    }

    /// <summary>Grants access to ONE lesson (its videos only). Its period follows the lesson's course.</summary>
    [HttpPost("lesson-enrollments")]
    [ProducesResponseType(typeof(LessonEnrollmentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LessonEnrollmentResponse>> GrantLesson(
        GrantLessonAccessRequest request,
        CancellationToken cancellationToken)
    {
        var result = await enrollments.GrantLessonAsync(request, StaffId(), cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : result.ToActionResult(this);
    }

    /// <summary>Everything a customer holds: course enrollments and single-lesson access, any status.</summary>
    [HttpGet("customers/{customerId:long}/access")]
    [ProducesResponseType(typeof(CustomerAccessResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAccessResponse>> GetAccess(long customerId, CancellationToken cancellationToken) =>
        (await enrollments.GetCustomerAccessAsync(customerId, cancellationToken)).ToActionResult(this);

    /// <summary>What this course costs this customer: list price, discounts for included courses they already have, final price.</summary>
    [HttpGet("customers/{customerId:long}/quote")]
    [ProducesResponseType(typeof(CourseQuoteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseQuoteResponse>> Quote(
        long customerId,
        [FromQuery] long courseId,
        CancellationToken cancellationToken) =>
        (await enrollments.QuoteCourseAsync(courseId, customerId, cancellationToken)).ToActionResult(this);

    [HttpPost("enrollments/{id:long}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RevokeCourse(long id, CancellationToken cancellationToken) =>
        (await enrollments.RevokeCourseAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("lesson-enrollments/{id:long}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> RevokeLesson(long id, CancellationToken cancellationToken) =>
        (await enrollments.RevokeLessonAsync(id, cancellationToken)).ToActionResult(this);

    private long? StaffId() => currentUser.TryGetStaffId(out var id) ? id : null;
}
