using Microsoft.AspNetCore.Http;

namespace ChefLan.Commons.Auth;

/// <summary>
/// Scoped accessor injected everywhere. Reads what the middleware computed; if the
/// middleware has not run (seeder, background work) it is an unauthenticated caller.
/// Anonymous endpoints are never authorized, so for them the middleware sees an empty
/// principal — which is correct.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private static readonly CurrentUser Anonymous = new(null);

    private ICurrentUser Inner =>
        accessor.HttpContext?.Items[CurrentUserMiddleware.ItemKey] as ICurrentUser ?? Anonymous;

    public ActorType? Source => Inner.Source;

    public bool IsAuthenticated => Inner.IsAuthenticated;

    public string? Email => Inner.Email;

    public string? Role => Inner.Role;

    public bool IsStaff => Inner.IsStaff;

    public bool IsCustomer => Inner.IsCustomer;

    public bool TryGetStaffId(out long userId) => Inner.TryGetStaffId(out userId);

    public bool TryGetCustomerId(out long customerId) => Inner.TryGetCustomerId(out customerId);
}
