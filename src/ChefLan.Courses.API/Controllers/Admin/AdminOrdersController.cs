using ChefLan.Commons.Auth;
using ChefLan.Commons.Pagination;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;
using ChefLan.Logic.Courses.Contracts.Responses;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChefLan.Courses.API.Controllers.Admin;

/// <summary>
/// Staff side of online payments: see every order and every bank transaction, and settle by hand what the automatic
/// matching could not. <c>GET payments?status=flagged</c> lists what needs a human.
/// </summary>
[ApiController]
[Route("api/v1/admins")]
[Authorize(Policy = ChefLanPolicies.StaffOrAdmin)]
[Produces("application/json")]
public sealed class AdminOrdersController(IOrderService orders, IPaymentService payments, ICurrentUser currentUser) : ControllerBase
{
    private static readonly PaymentMatchStatus[] Flagged =
    [
        PaymentMatchStatus.Unmatched,
        PaymentMatchStatus.Underpaid,
        PaymentMatchStatus.LateAfterExpiry,
        PaymentMatchStatus.Duplicate,
        PaymentMatchStatus.Overpaid,
    ];

    /// <summary>
    /// Orders, newest first. <c>status</c> is the status the response shows (a Pending order past its deadline is
    /// Expired); <c>paymentMethod</c> is BankTransfer or ContactChef; <c>customerId</c> narrows to one customer;
    /// <c>q</c> matches the order code or the customer's name, email or phone.
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(PagedResult<AdminOrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminOrderResponse>>> ListOrders(
        [FromQuery] AdminOrderFilter filter,
        [FromQuery] PagedRequest paging,
        CancellationToken cancellationToken) =>
        Ok(await orders.ListAsync(filter, paging, cancellationToken));

    /// <summary>
    /// Confirms a payment by hand: grants the access. A reason is required and recorded with who did it. Pass a bank
    /// transaction id to attach it. 409 if the order is already paid.
    /// </summary>
    [HttpPost("orders/{id:long}/mark-paid")]
    [ProducesResponseType(typeof(AdminOrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminOrderResponse>> MarkPaid(long id, MarkOrderPaidRequest request, CancellationToken cancellationToken) =>
        (await orders.MarkPaidAsync(id, request, StaffId(), cancellationToken)).ToActionResult(this);

    [HttpPost("orders/{id:long}/cancel")]
    [ProducesResponseType(typeof(AdminOrderResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminOrderResponse>> Cancel(long id, CancellationToken cancellationToken) =>
        (await orders.CancelAsync(id, cancellationToken)).ToActionResult(this);

    /// <summary>
    /// Bank transactions, newest first. <c>status</c> is <c>flagged</c> (everything that needs staff), <c>all</c>, or one
    /// of Unmatched, Matched, Overpaid, Underpaid, LateAfterExpiry, Duplicate, Ignored.
    /// </summary>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(PagedResult<PaymentTransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentTransactionResponse>>> ListPayments(
        [FromQuery] string? status,
        [FromQuery] PagedRequest paging,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PaymentMatchStatus>? statuses;

        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            statuses = null;
        }
        else if (status.Equals("flagged", StringComparison.OrdinalIgnoreCase))
        {
            statuses = Flagged;
        }
        else if (Enum.TryParse<PaymentMatchStatus>(status, ignoreCase: true, out var one) && Enum.IsDefined(one))
        {
            statuses = [one];
        }
        else
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["status"] = ["Use flagged, all, or a match status name."] }));
        }

        return Ok(await payments.ListAsync(statuses, paging, cancellationToken));
    }

    /// <summary>Attaches a bank transaction to an order; an unpaid order is paid and its access granted.</summary>
    [HttpPost("payments/{id:long}/link")]
    [ProducesResponseType(typeof(PaymentTransactionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentTransactionResponse>> Link(long id, LinkPaymentRequest request, CancellationToken cancellationToken) =>
        (await payments.LinkAsync(id, request.OrderId, StaffId(), cancellationToken)).ToActionResult(this);

    private long? StaffId() => currentUser.TryGetStaffId(out var id) ? id : null;
}
