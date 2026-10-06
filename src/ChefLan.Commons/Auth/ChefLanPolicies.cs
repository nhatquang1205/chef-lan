namespace ChefLan.Commons.Auth;

public static class ChefLanPolicies
{
    /// <summary>Internal scheme, Staff actor, Admin role.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Internal scheme, Staff actor, Admin or Staff role.</summary>
    public const string StaffOrAdmin = "StaffOrAdmin";

    /// <summary>Customer scheme, Customer actor.</summary>
    public const string CustomerOnly = "CustomerOnly";
}
