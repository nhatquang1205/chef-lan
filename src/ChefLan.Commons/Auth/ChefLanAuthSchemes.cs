namespace ChefLan.Commons.Auth;

/// <summary>
/// Two JWT bearer schemes, deliberately isolated. "sub" means a staff user id under
/// <see cref="Internal"/> and a customer id under <see cref="Customer"/>, so the two must
/// never validate each other's tokens.
/// </summary>
public static class ChefLanAuthSchemes
{
    public const string Internal = "Internal";

    public const string Customer = "Customer";

    /// <summary>Configuration section names: Jwt:Internal and Jwt:Customer.</summary>
    public const string ConfigurationRoot = "Jwt";
}
