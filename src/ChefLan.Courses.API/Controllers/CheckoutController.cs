using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/checkout")]
[Produces("application/json")]
public sealed class CheckoutController(IOrderService orders) : ControllerBase
{
    /// <summary>Whether online payment is set up. The website shows "Buy now" only when it is.</summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(CheckoutConfigResponse), StatusCodes.Status200OK)]
    public ActionResult<CheckoutConfigResponse> GetConfig() => orders.GetConfig();
}
