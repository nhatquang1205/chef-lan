namespace ChefLan.Commons.Auth;

public interface IJwtTokenService
{
    /// <summary>Signs a staff access token with the Internal scheme's key and audience.</summary>
    AccessToken CreateStaffToken(StaffTokenData data);

    /// <summary>Signs a customer access token with the Customer scheme's key and audience.</summary>
    AccessToken CreateCustomerToken(CustomerTokenData data);

    /// <summary>Refresh-token lifetime for a scheme, from that scheme's options.</summary>
    TimeSpan RefreshTokenLifetime(string scheme);
}
