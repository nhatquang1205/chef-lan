using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ChefLan.Commons.Auth;

public sealed class HttpCurrentStaffContext(IHttpContextAccessor accessor) : ICurrentStaffContext
{
    public bool IsAuthenticated => Principal is not null;

    public long? UserId =>
        long.TryParse(Principal?.FindFirst(ChefLanClaimTypes.Subject)?.Value, out var id) ? id : null;

    public string? Email => Principal?.FindFirst(ChefLanClaimTypes.Email)?.Value;

    public string? Role => Principal?.FindFirst(ChefLanClaimTypes.Role)?.Value;

    public bool IsAdmin => Role == ChefLanRoles.Admin;

    public long RequireUserId() =>
        UserId ?? throw new InvalidOperationException(
            "No authenticated staff user on this request. Is the endpoint using the Internal scheme?");

    /// <summary>
    /// Returns the principal only when it carries the Staff actor claim. A customer token
    /// reaching this accessor yields null rather than a customer id masquerading as a user id.
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

            return user.FindFirst(ChefLanClaimTypes.ActorType)?.Value == TokenActorType.Staff ? user : null;
        }
    }
}
