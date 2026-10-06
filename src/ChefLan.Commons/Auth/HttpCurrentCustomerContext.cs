using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ChefLan.Commons.Auth;

public sealed class HttpCurrentCustomerContext(IHttpContextAccessor accessor) : ICurrentCustomerContext
{
    public bool IsAuthenticated => Principal is not null;

    public long? CustomerId =>
        long.TryParse(Principal?.FindFirst(ChefLanClaimTypes.Subject)?.Value, out var id) ? id : null;

    public string? Email => Principal?.FindFirst(ChefLanClaimTypes.Email)?.Value;

    public long RequireCustomerId() =>
        CustomerId ?? throw new InvalidOperationException(
            "No authenticated customer on this request. Is the endpoint using the Customer scheme?");

    /// <summary>
    /// Returns the principal only when it carries the Customer actor claim, so a staff
    /// token can never be read as a customer id.
    /// </summary>
    private ClaimsPrincipal? Principal
    {
        get
        {
            var user = accessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirst(ChefLanClaimTypes.ActorType)?.Value == TokenActorType.Customer ? user : null;
        }
    }
}
