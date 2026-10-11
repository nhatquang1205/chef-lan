using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize(Policy = ChefLanPolicies.CustomerOnly)]
[Produces("application/json")]
public sealed class MeController(ICustomerAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    public const string GetRouteName = "GetCurrentCustomer";

    [HttpGet(Name = GetRouteName)]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> Get(CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        return (await auth.GetMeAsync(customerId, cancellationToken)).ToActionResult(this);
    }
}
