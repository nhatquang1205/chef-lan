using System.Text.Json.Serialization;
using ChefLan.Commons.Auth;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Identity.Contracts;

namespace ChefLan.Courses.API.Contracts;

/// <summary>
/// The result of signing in through the shared entry point (/api/v1/auth/login and /refresh).
/// <c>Actor</c> says which population the token belongs to; exactly one of <c>Customer</c> and
/// <c>Staff</c> is present. The token itself is a normal Customer or Internal token.
/// </summary>
public sealed record SessionResponse(
    ActorType Actor,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] CustomerResponse? Customer,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] StaffResponse? Staff)
{
    public static SessionResponse From(CustomerAuthResponse r) =>
        new(ActorType.Customer, r.AccessToken, r.AccessTokenExpiresAt, r.RefreshToken, r.Customer, null);

    public static SessionResponse From(StaffAuthResponse r) =>
        new(ActorType.Staff, r.AccessToken, r.AccessTokenExpiresAt, r.RefreshToken, null, r.User);
}

public sealed record MeResponse(
    ActorType Actor,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] CustomerResponse? Customer,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] StaffResponse? Staff);
