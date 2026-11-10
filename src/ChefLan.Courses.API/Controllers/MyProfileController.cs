using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[Authorize(Policy = ChefLanPolicies.CustomerOnly)]
[Route("api/v1/me/profile")]
[Produces("application/json")]
public sealed class MyProfileController(ICustomerAuthService customers, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Name, phone and province in one call; the website sends a customer here until both are set.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> Update(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        var result = await customers.UpdateProfileAsync(customerId, request, cancellationToken);

        return result.IsSuccess ? result.Value : result.ToActionResult(this).Result!;
    }
}
