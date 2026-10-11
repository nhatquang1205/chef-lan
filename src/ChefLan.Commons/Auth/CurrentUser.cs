using System.Security.Claims;

namespace ChefLan.Commons.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(ClaimsPrincipal? principal)
    {
        _principal = principal?.Identity?.IsAuthenticated == true ? principal : null;
        Source = _principal?.FindFirst(ChefLanClaimTypes.ActorType)?.Value switch
        {
            TokenActorType.Staff => ActorType.Staff,
            TokenActorType.Customer => ActorType.Customer,
            _ => null,
        };

        // An authenticated principal without a recognised "act" claim is treated as nobody.
        if (Source is null)
        {
            _principal = null;
        }
    }

    public ActorType? Source { get; }

    public bool IsAuthenticated => _principal is not null;

    public string? Email => _principal?.FindFirst(ChefLanClaimTypes.Email)?.Value;

    public string? Role => IsStaff ? _principal!.FindFirst(ChefLanClaimTypes.Role)?.Value : null;

    public bool IsStaff => Source == ActorType.Staff;

    public bool IsCustomer => Source == ActorType.Customer;

    public bool TryGetStaffId(out long userId) => TryGetSubject(IsStaff, out userId);

    public bool TryGetCustomerId(out long customerId) => TryGetSubject(IsCustomer, out customerId);

    private bool TryGetSubject(bool expected, out long id)
    {
        id = 0;
        return expected && long.TryParse(_principal!.FindFirst(ChefLanClaimTypes.Subject)?.Value, out id);
    }
}
