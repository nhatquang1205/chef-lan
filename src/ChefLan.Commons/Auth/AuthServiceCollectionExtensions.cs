using System.Text;
using ChefLan.Commons.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ChefLan.Commons.Auth;

public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers BOTH jwt bearer schemes, each with its own signing key, audience and
    /// lifetime. Configuration lives under Jwt:Internal and Jwt:Customer.
    /// </summary>
    public static IServiceCollection AddChefLanAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        BindScheme(services, configuration, ChefLanAuthSchemes.Internal, ChefLanAudiences.Internal);
        BindScheme(services, configuration, ChefLanAuthSchemes.Customer, ChefLanAudiences.Customer);

        services
            .AddAuthentication(ChefLanAuthSchemes.Customer)
            .AddJwtBearer(ChefLanAuthSchemes.Internal, o =>
                ConfigureBearer(o, configuration, ChefLanAuthSchemes.Internal, ChefLanAudiences.Internal))
            .AddJwtBearer(ChefLanAuthSchemes.Customer, o =>
                ConfigureBearer(o, configuration, ChefLanAuthSchemes.Customer, ChefLanAudiences.Customer));

        return services;
    }

    /// <summary>
    /// Every policy pins its authentication scheme, so a customer token presented to an
    /// admin route fails authentication outright instead of falling through to a role check.
    /// </summary>
    public static IServiceCollection AddChefLanAuthorization(this IServiceCollection services)
    {
        services
            .AddAuthorizationBuilder()
            .AddPolicy(ChefLanPolicies.AdminOnly, p => p
                .AddAuthenticationSchemes(ChefLanAuthSchemes.Internal)
                .RequireAuthenticatedUser()
                .RequireClaim(ChefLanClaimTypes.ActorType, TokenActorType.Staff)
                .RequireRole(ChefLanRoles.Admin))
            .AddPolicy(ChefLanPolicies.StaffOrAdmin, p => p
                .AddAuthenticationSchemes(ChefLanAuthSchemes.Internal)
                .RequireAuthenticatedUser()
                .RequireClaim(ChefLanClaimTypes.ActorType, TokenActorType.Staff)
                .RequireRole(ChefLanRoles.Admin, ChefLanRoles.Staff))
            .AddPolicy(ChefLanPolicies.AnyUser, p => p
                .AddAuthenticationSchemes(ChefLanAuthSchemes.Internal, ChefLanAuthSchemes.Customer)
                .RequireAuthenticatedUser()
                .RequireClaim(ChefLanClaimTypes.ActorType, TokenActorType.Staff, TokenActorType.Customer))
            .AddPolicy(ChefLanPolicies.CustomerOnly, p => p
                .AddAuthenticationSchemes(ChefLanAuthSchemes.Customer)
                .RequireAuthenticatedUser()
                .RequireClaim(ChefLanClaimTypes.ActorType, TokenActorType.Customer));

        return services;
    }

    private static void BindScheme(
        IServiceCollection services,
        IConfiguration configuration,
        string scheme,
        string defaultAudience)
    {
        services
            .AddOptions<JwtOptions>(scheme)
            .Bind(configuration.GetSection($"{ChefLanAuthSchemes.ConfigurationRoot}:{scheme}"))
            .PostConfigure(o =>
            {
                if (string.IsNullOrWhiteSpace(o.Audience))
                {
                    o.Audience = defaultAudience;
                }
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void ConfigureBearer(
        JwtBearerOptions bearer,
        IConfiguration configuration,
        string scheme,
        string defaultAudience)
    {
        var section = configuration.GetSection($"{ChefLanAuthSchemes.ConfigurationRoot}:{scheme}");
        var signingKey = section["SigningKey"]
            ?? throw new InvalidOperationException(
                $"Missing Jwt:{scheme}:SigningKey. Set it with dotnet user-secrets, and use a "
                + "DIFFERENT value for each scheme.");
        var issuer = section["Issuer"] ?? "cheflan";
        var audience = section["Audience"] is { Length: > 0 } a ? a : defaultAudience;
        var skew = int.TryParse(section["ClockSkewSeconds"], out var s) ? s : 30;

        // Keep the short claim names ("sub", "role", "act") instead of the legacy SOAP URIs.
        bearer.MapInboundClaims = false;

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(skew),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = ChefLanClaimTypes.Name,
            RoleClaimType = ChefLanClaimTypes.Role,
        };

        bearer.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.Response.Headers["X-ChefLan-Auth-Scheme"] = scheme;
                return Task.CompletedTask;
            },
        };
    }
}
