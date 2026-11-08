using ChefLan.Commons.Auth;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[Authorize(Policy = ChefLanPolicies.CustomerOnly)]
[Route("api/v1/me/courses")]
[Produces("application/json")]
public sealed class MyCoursesController(IEnrollmentService enrollments, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// The signed-in customer's library: every published course they own whole or in part (active,
    /// unexpired access only), with the lessons they own individually. Newest grant first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MyCourseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyCourseResponse>>> List(CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        return Ok(await enrollments.ListMyCoursesAsync(customerId, cancellationToken));
    }

    /// <summary>
    /// What the website needs to personalise the catalog: the ids of the courses the signed-in customer owns whole
    /// (directly or through a course that includes them), and the reduced price of each published course they do not
    /// own whose price drops because of shared modules they own. Never cached.
    /// </summary>
    [HttpGet("~/api/v1/me/catalog-state")]
    [ProducesResponseType(typeof(MyCatalogStateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyCatalogStateResponse>> CatalogState(CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        Response.Headers.CacheControl = "private, no-store";

        return Ok(await enrollments.GetMyCatalogStateAsync(customerId, cancellationToken));
    }
}
