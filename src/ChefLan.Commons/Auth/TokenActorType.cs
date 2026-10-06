namespace ChefLan.Commons.Auth;

/// <summary>
/// String values for the "act" claim. Commons cannot reference ChefLan.Domain, so the
/// values are duplicated here and must stay in sync with ChefLan.Domain.Enums.ActorType.
/// </summary>
public static class TokenActorType
{
    public const string Staff = "Staff";

    public const string Customer = "Customer";
}
