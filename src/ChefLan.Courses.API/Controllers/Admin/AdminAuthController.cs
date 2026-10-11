using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Identity.Contracts;
using ChefLan.Logic.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChefLan.Courses.API.Controllers.Admin;

/// <summary>Staff login. There is no staff registration endpoint — an Admin creates accounts.</summary>
[ApiController]
[Route("api/v1/admins/auth")]
[Produces("application/json")]
public sealed class AdminAuthController(IStaffAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    [ProducesResponseType(typeof(StaffAuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StaffAuthResponse>> Login(
        StaffLoginRequest request,
        CancellationToken cancellationToken) =>
        (await auth.LoginAsync(request, HttpContext.GetClientInfo(), cancellationToken)).ToActionResult(this);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthRefresh)]
    [ProducesResponseType(typeof(StaffAuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StaffAuthResponse>> Refresh(
        StaffRefreshRequest request,
        CancellationToken cancellationToken) =>
        (await auth.RefreshAsync(request, HttpContext.GetClientInfo(), cancellationToken)).ToActionResult(this);

    [HttpPost("logout")]
    [Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout(StaffLogoutRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetStaffId(out var userId))
        {
            return Unauthorized();
        }

        return (await auth.LogoutAsync(userId, request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Ends every session, including this one: log in again with the new password.</summary>
    [HttpPost("change-password")]
    [Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ChangePassword(
        StaffChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetStaffId(out var userId))
        {
            return Unauthorized();
        }

        return (await auth.ChangePasswordAsync(userId, request, cancellationToken)).ToActionResult(this);
    }
}
