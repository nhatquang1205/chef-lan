using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Identity.Contracts;
using ChefLan.Logic.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admins/me")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminMeController(IStaffAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(StaffResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StaffResponse>> Get(CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetStaffId(out var userId))
        {
            return Unauthorized();
        }

        return (await auth.GetMeAsync(userId, cancellationToken)).ToActionResult(this);
    }
}
