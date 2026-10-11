using ChefLan.Commons.Auth;
using ChefLan.Commons.Results;
using ChefLan.Courses.API.Contracts;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Errors;
using ChefLan.Logic.Customers.Interfaces;
using ChefLan.Logic.Identity.Contracts;
using ChefLan.Logic.Identity.Errors;
using ChefLan.Logic.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChefLan.Courses.API.Controllers;

/// <summary>
/// The shared entry point for the web client. Customers register and sign in here; an ADMIN may
/// also sign in here with the staff credentials and gets a normal Internal (staff) token — the web
/// client then uses one login form for both. Tokens stay separate: a staff token is signed with the
/// staff key and carries act=Staff, so it still cannot be mistaken for a customer id.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class CustomerAuthController(
    ICustomerAuthService customers,
    IStaffAuthService staff,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthRegister)]
    [ProducesResponseType(typeof(CustomerAuthResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerAuthResponse>> Register(
        CustomerRegisterRequest request,
        CancellationToken cancellationToken) =>
        (await customers.RegisterAsync(request, HttpContext.GetClientInfo(), cancellationToken))
            .ToCreatedResult(this, MeController.GetRouteName, new { });

    /// <summary>
    /// Customer credentials first; if the email/password are not a customer's, the same pair is tried
    /// as a staff account. Every failing path costs two password verifications (a miss verifies a
    /// dummy hash), so response time does not reveal which population an email belongs to.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionResponse>> Login(
        CustomerLoginRequest request,
        CancellationToken cancellationToken)
    {
        var client = HttpContext.GetClientInfo();
        var asCustomer = await customers.LoginAsync(request, client, cancellationToken);

        if (asCustomer.IsSuccess)
        {
            return SessionResponse.From(asCustomer.Value);
        }

        // Locked, inactive or not activated: a real customer account answered, so stop there.
        if (asCustomer.Error != CustomerErrors.InvalidCredentials)
        {
            return Result.Failure(asCustomer.Error).ToActionResult(this);
        }

        var asStaff = await staff.LoginAsync(new StaffLoginRequest(request.Email, request.Password), client, cancellationToken);

        if (asStaff.IsSuccess)
        {
            return SessionResponse.From(asStaff.Value);
        }

        // Neither population matched: the same generic 401 as before. A staff-specific answer
        // (locked, inactive) is only given when the staff credentials WERE right.
        return Result.Failure(asStaff.Error == IdentityErrors.InvalidCredentials ? asCustomer.Error : asStaff.Error)
            .ToActionResult(this);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthRefresh)]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionResponse>> Refresh(
        CustomerRefreshRequest request,
        CancellationToken cancellationToken)
    {
        var client = HttpContext.GetClientInfo();
        var asCustomer = await customers.RefreshAsync(request, client, cancellationToken);

        if (asCustomer.IsSuccess)
        {
            return SessionResponse.From(asCustomer.Value);
        }

        // Refresh tokens are looked up by hash in separate tables, so a staff token is simply "not
        // found" in the customer table (no side effects) and is then tried against the staff table.
        var asStaff = await staff.RefreshAsync(new StaffRefreshRequest(request.RefreshToken), client, cancellationToken);

        return asStaff.IsSuccess
            ? SessionResponse.From(asStaff.Value)
            : Result.Failure(asCustomer.Error).ToActionResult(this);
    }

    [HttpPost("logout")]
    [Authorize(Policy = ChefLanPolicies.AnyUser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout(CustomerLogoutRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.TryGetCustomerId(out var customerId))
        {
            return (await customers.LogoutAsync(customerId, request, cancellationToken)).ToActionResult(this);
        }

        if (currentUser.TryGetStaffId(out var userId))
        {
            return (await staff.LogoutAsync(userId, new StaffLogoutRequest(request.RefreshToken), cancellationToken))
                .ToActionResult(this);
        }

        return Unauthorized();
    }
}
