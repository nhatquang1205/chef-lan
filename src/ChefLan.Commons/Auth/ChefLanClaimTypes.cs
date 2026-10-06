namespace ChefLan.Commons.Auth;

/// <summary>
/// Short claim names. MapInboundClaims is turned off so these survive validation instead
/// of being rewritten to the legacy SOAP URIs.
/// </summary>
public static class ChefLanClaimTypes
{
    public const string Subject = "sub";

    public const string Email = "email";

    public const string Name = "name";

    public const string Role = "role";

    /// <summary>ActorType: "Staff" or "Customer". Required by every authorization policy.</summary>
    public const string ActorType = "act";
}
