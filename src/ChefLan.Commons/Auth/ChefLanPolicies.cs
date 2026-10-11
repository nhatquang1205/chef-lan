namespace ChefLan.Commons.Auth;

public static class ChefLanPolicies
{
    /// <summary>Internal scheme, Staff actor, Admin role.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Internal scheme, Staff actor, Admin or Staff role.</summary>
    public const string StaffOrAdmin = "StaffOrAdmin";

    /// <summary>
    /// EITHER scheme, Staff or Customer actor. For the shared account endpoints (/auth/logout, /me)
    /// where an admin signed in through the customer entry point must also be recognised. The tokens
    /// stay separate (own key, audience, act claim); only the entry point is shared.
    /// </summary>
    public const string AnyUser = "AnyUser";

    /// <summary>Customer scheme, Customer actor.</summary>
    public const string CustomerOnly = "CustomerOnly";
}
