using System.Text;
using ChefLan.Courses.API.Infrastructure;
using ChefLan.Logic.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ChefLan.Courses.API.Controllers;

/// <summary>Where the payment provider tells us a bank transfer arrived.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/payments")]
[Produces("application/json")]
public sealed class PaymentsController(IPaymentService payments, IPaymentSettings settings) : ControllerBase
{
    private const int MaxBodyBytes = 64 * 1024;

    /// <summary>
    /// SePay Payment Gateway's notification endpoint (set it as the IPN URL, with the IPN secret). Authenticated by the
    /// secret in the <c>X-Secret-Key</c> header; there is no user. Answers 200 once the notification is stored,
    /// whether or not it matched an order, so the provider does not retry forever. A transient failure answers 5xx and
    /// the provider retries: safe, because the transaction id makes processing idempotent.
    /// </summary>
    [HttpPost("sepay/ipn")]
    [EnableRateLimiting(RateLimitPolicies.PaymentsIpn)]
    [RequestSizeLimit(MaxBodyBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SePayIpn(CancellationToken cancellationToken)
    {
        if (!settings.CheckoutEnabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false });
        }

        if (!settings.VerifyIpnSecret(PresentedSecret()))
        {
            return Unauthorized(new { success = false });
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var result = await payments.IngestSePayAsync(body, cancellationToken);

        return result.IsSuccess
            ? Ok(new { success = true })
            : result.ToActionResult(this).Result!;
    }

    /// <summary>The IPN secret from SePay's <c>X-Secret-Key</c> header, or from <c>Authorization: Apikey {secret}</c> if the IPN is set up with an API key.</summary>
    private string? PresentedSecret()
    {
        var secret = Request.Headers["X-Secret-Key"].FirstOrDefault();

        if (!string.IsNullOrEmpty(secret))
        {
            return secret;
        }

        var authorization = Request.Headers.Authorization.ToString();
        const string prefix = "Apikey ";

        return authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[prefix.Length..].Trim()
            : Request.Headers["X-Api-Key"].FirstOrDefault();
    }
}
