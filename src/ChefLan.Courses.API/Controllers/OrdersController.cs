using ChefLan.Commons.Auth;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChefLan.Courses.API.Controllers;

/// <summary>A customer buying a course or a lesson by bank transfer. Staff accounts cannot buy: they have no customer id.</summary>
[ApiController]
[Authorize(Policy = ChefLanPolicies.CustomerOnly)]
[Route("api/v1")]
[Produces("application/json")]
public sealed class OrdersController(IOrderService orders, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Starts a purchase of a course (<c>courseSlug</c>) or a lesson (<c>lessonId</c>). The price is decided by the
    /// server. Asking again for the same item returns the order that is already waiting for payment. 409 if the customer
    /// already has the item, 503 when online payment is not set up.
    /// </summary>
    [HttpPost("orders")]
    [EnableRateLimiting(RateLimitPolicies.OrdersCreate)]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        NoStore();

        return (await orders.CreateAsync(customerId, request, cancellationToken)).ToActionResult(this);
    }

    /// <summary>The order's current state. The website polls this while the customer is paying.</summary>
    [HttpGet("orders/{code}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Get(string code, CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        NoStore();

        return (await orders.GetAsync(customerId, code, cancellationToken)).ToActionResult(this);
    }

    [HttpPost("orders/{code}/cancel")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderResponse>> Cancel(string code, CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        NoStore();

        return (await orders.CancelAsync(customerId, code, cancellationToken)).ToActionResult(this);
    }

    /// <summary>The customer's recent orders, newest first.</summary>
    [HttpGet("me/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> ListMine(CancellationToken cancellationToken)
    {
        if (!currentUser.TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        NoStore();

        return Ok(await orders.ListMineAsync(customerId, cancellationToken));
    }

    // Payment details and a status that changes: never cache.
    private void NoStore() => Response.Headers.CacheControl = "private, no-store";
}
