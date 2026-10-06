using System.Security.Claims;
using System.Text;
using ChefLan.Commons.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ChefLan.Commons.Auth;

public sealed class JwtTokenService(IOptionsMonitor<JwtOptions> options, IClock clock) : IJwtTokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateStaffToken(StaffTokenData data) =>
        Create(
            ChefLanAuthSchemes.Internal,
            TokenActorType.Staff,
            data.UserId,
            data.Email,
            data.DisplayName,
            data.Role);

    public AccessToken CreateCustomerToken(CustomerTokenData data) =>
        Create(
            ChefLanAuthSchemes.Customer,
            TokenActorType.Customer,
            data.CustomerId,
            data.Email,
            data.FullName,
            role: null);

    public TimeSpan RefreshTokenLifetime(string scheme) =>
        TimeSpan.FromDays(options.Get(scheme).RefreshTokenDays);

    private AccessToken Create(
        string scheme,
        string actorType,
        long subject,
        string email,
        string name,
        string? role)
    {
        var o = options.Get(scheme);
        var issuedAt = clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(o.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ChefLanClaimTypes.Subject, subject.ToString()),
            new(ChefLanClaimTypes.Email, email),
            new(ChefLanClaimTypes.Name, name),
            new(ChefLanClaimTypes.ActorType, actorType),
        };

        if (role is not null)
        {
            claims.Add(new Claim(ChefLanClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = o.Issuer,
            Audience = o.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
