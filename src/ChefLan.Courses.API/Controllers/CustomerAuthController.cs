using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class CustomerAuthController(ICustomerAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthRegister)]
    [ProducesResponseType(typeof(CustomerAuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerAuthResponse>> Register(
        CustomerRegisterRequest request,
        CancellationToken cancellationToken) =>
        (await auth.RegisterAsync(request, HttpContext.GetClientInfo(), cancellationToken))
            .ToCreatedResult(this, MeController.GetRouteName, new { });

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    [ProducesResponseType(typeof(CustomerAuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAuthResponse>> Login(
        CustomerLoginRequest request,
        CancellationToken cancellationToken) =>
        (await auth.LoginAsync(request, HttpContext.GetClientInfo(), cancellationToken)).ToActionResult(this);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthRefresh)]
    [ProducesResponseType(typeof(CustomerAuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAuthResponse>> Refresh(
        CustomerRefreshRequest request,
        CancellationToken cancellationToken) =>
        (await auth.RefreshAsync(request, HttpContext.GetClientInfo(), cancellationToken)).ToActionResult(this);

    [HttpPost("logout")]
    [Authorize(Policy = ChefLanPolicies.CustomerOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout(CustomerLogoutRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        return (await auth.LogoutAsync(customerId, request, cancellationToken)).ToActionResult(this);
    }
}
