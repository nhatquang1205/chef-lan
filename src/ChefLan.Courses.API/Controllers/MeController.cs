using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Contracts;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Customers.Interfaces;
using ChefLan.Logic.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

/// <summary>Who am I, for a customer or an admin signed in through /api/v1/auth/login.</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize(Policy = ChefLanPolicies.AnyUser)]
[Produces("application/json")]
public sealed class MeController(
    ICustomerAuthService customers,
    IStaffAuthService staff,
    ICurrentUser currentUser) : ControllerBase
{
    public const string GetRouteName = "GetCurrentCustomer";

    [HttpGet(Name = GetRouteName)]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        if (currentUser.TryGetCustomerId(out var customerId))
        {
            var customer = await customers.GetMeAsync(customerId, cancellationToken);

            return customer.IsSuccess
                ? new MeResponse(ActorType.Customer, customer.Value, null)
                : customer.ToActionResult(this).Result!;
        }

        if (currentUser.TryGetStaffId(out var userId))
        {
            var user = await staff.GetMeAsync(userId, cancellationToken);

            return user.IsSuccess
                ? new MeResponse(ActorType.Staff, null, user.Value)
                : user.ToActionResult(this).Result!;
        }

        return Unauthorized();
    }
}
