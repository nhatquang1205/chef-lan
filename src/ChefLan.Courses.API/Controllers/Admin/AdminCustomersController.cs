using ChefLan.Commons.Auth;
using ChefLan.Commons.Pagination;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admins/customers")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminCustomersController(ICustomerAdminService customers, ICurrentUser currentUser) : ControllerBase
{
    private const string GetRouteName = "AdminGetCustomer";

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerAdminListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CustomerAdminListItem>>> List(
        [FromQuery] string? q,
        [FromQuery] CustomerStatus? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        Ok(await customers.ListAsync(new CustomerAdminQuery(q, status, page, pageSize), cancellationToken));

    [HttpGet("{id:long}", Name = GetRouteName)]
    public async Task<ActionResult<CustomerAdminDetail>> Get(long id, CancellationToken cancellationToken) =>
        (await customers.GetAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost]
    [ProducesResponseType(typeof(CustomerAdminDetail), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerAdminDetail>> Create(
        CustomerAdminRequest request,
        CancellationToken cancellationToken)
    {
        long? staffId = currentUser.TryGetStaffId(out var id) ? id : null;
        var result = await customers.CreateAsync(request, staffId, cancellationToken);

        return result.ToCreatedResult(this, GetRouteName, new { id = result.IsSuccess ? result.Value.Id : 0 });
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CustomerAdminDetail>> Update(
        long id,
        CustomerAdminRequest request,
        CancellationToken cancellationToken) =>
        (await customers.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Sends (or resends) the invitation mail; the new link replaces any earlier one and works for 7 days.
    /// 502 <c>email.send_failed</c> when the mail could not be sent (nothing changes then).
    /// </summary>
    [HttpPost("{id:long}/invite")]
    [ProducesResponseType(typeof(CustomerInviteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerInviteResponse>> Invite(long id, CancellationToken cancellationToken) =>
        (await customers.InviteAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>Signs the customer out everywhere (all refresh tokens revoked) and blocks login.</summary>
    [HttpPost("{id:long}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Deactivate(long id, CancellationToken cancellationToken) =>
        (await customers.DeactivateAsync(id, cancellationToken)).ToActionResult(this);

    [HttpPost("{id:long}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Reactivate(long id, CancellationToken cancellationToken) =>
        (await customers.ReactivateAsync(id, cancellationToken)).ToActionResult(this);
}
