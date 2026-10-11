using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;

namespace ChefLan.Courses.API.Infrastructure;

public static class RateLimitPolicies
{
    public const string AuthLogin = "auth-login";

    public const string AuthRegister = "auth-register";

    public const string AuthRefresh = "auth-refresh";
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Two dimensions per policy: the client IP (named policy, applied by
/// [EnableRateLimiting]) and the submitted email (a global limiter keyed by the same policy
/// name). Per-IP alone is beaten by a botnet; per-email alone by spraying accounts from one
/// host. Lockout protects one account, this protects the endpoint.
/// </summary>
public static class RateLimitingConfiguration
{
    private const string SectionName = "RateLimiting";
    private const string EmailItemKey = "ChefLan.RateLimit.Email";
    private const int MaxBodyBytesInspected = 4096;

    private static readonly Dictionary<string, RateLimitPolicyOptions> Defaults = new()
    {
        [RateLimitPolicies.AuthLogin] = new() { PermitLimit = 5, WindowSeconds = 60 },
        [RateLimitPolicies.AuthRegister] = new() { PermitLimit = 3, WindowSeconds = 60 },
        [RateLimitPolicies.AuthRefresh] = new() { PermitLimit = 20, WindowSeconds = 60 },
    };

    public static IServiceCollection AddChefLanRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var limits = Defaults.ToDictionary(
            d => d.Key,
            d => configuration.GetSection($"{SectionName}:{d.Key}").Get<RateLimitPolicyOptions>() ?? d.Value);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            foreach (var (name, limit) in limits)
            {
                options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
                    $"{name}|ip|{context.Connection.RemoteIpAddress}",
                    _ => Window(limit)));
            }

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var policy = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

                if (policy is null
                    || !limits.TryGetValue(policy, out var limit)
                    || context.Items[EmailItemKey] is not string email)
                {
                    return RateLimitPartition.GetNoLimiter("none");
                }

                return RateLimitPartition.GetFixedWindowLimiter($"{policy}|email|{email}", _ => Window(limit));
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay)
                    ? (int)Math.Ceiling(delay.TotalSeconds)
                    : 60;

                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                http.Response.Headers.RetryAfter = retryAfter.ToString();

                await http.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = $"Try again in {retryAfter} seconds.",
                        Instance = http.Request.Path,
                    },
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Enabled only when ForwardedHeaders:KnownProxies is configured. Trusting
    /// X-Forwarded-For from anyone lets a client mint a fresh IP partition per request.
    /// </summary>
    public static IServiceCollection AddChefLanForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var proxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];

        if (proxies.Length == 0)
        {
            return services;
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        services.AddSingleton<ForwardedHeadersEnabledMarker>();

        return services;
    }

    public static IApplicationBuilder UseChefLanForwardedHeaders(this WebApplication app)
    {
        if (app.Services.GetService<ForwardedHeadersEnabledMarker>() is not null)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }

    /// <summary>
    /// Reads the "email" field of a JSON body into HttpContext.Items so the (synchronous)
    /// limiter partitioner can use it. Only for endpoints that carry a rate-limit policy.
    /// </summary>
    public static IApplicationBuilder UseChefLanRateLimitEmail(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var request = context.Request;

            if (context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>() is not null
                && HttpMethods.IsPost(request.Method)
                && request.HasJsonContentType())
            {
                var email = await ReadEmailAsync(request, context.RequestAborted);

                if (email is not null)
                {
                    context.Items[EmailItemKey] = email;
                }
            }

            await next(context);
        });

    private static async Task<string?> ReadEmailAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        var buffer = new byte[MaxBodyBytesInspected];
        var read = 0;

        while (read < buffer.Length)
        {
            var n = await request.Body.ReadAsync(buffer.AsMemory(read), cancellationToken);

            if (n == 0)
            {
                break;
            }

            read += n;
        }

        request.Body.Position = 0;

        try
        {
            using var document = JsonDocument.Parse(buffer.AsMemory(0, read));

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Name.Equals("email", StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value.ValueKind == JsonValueKind.String
                            ? property.Value.GetString()?.Trim().ToLowerInvariant()
                            : null;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed or truncated: the IP partition still applies, model binding returns 400.
        }

        return null;
    }

    private static FixedWindowRateLimiterOptions Window(RateLimitPolicyOptions limit) => new()
    {
        PermitLimit = limit.PermitLimit,
        Window = TimeSpan.FromSeconds(limit.WindowSeconds),
        QueueLimit = 0,
        AutoReplenishment = true,
    };

    private sealed class ForwardedHeadersEnabledMarker;
}
