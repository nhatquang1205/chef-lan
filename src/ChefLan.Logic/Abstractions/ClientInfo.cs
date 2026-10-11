namespace ChefLan.Logic.Abstractions;

/// <summary>Where a request came from. Stored on refresh tokens for audit only.</summary>
public sealed record ClientInfo(string? IpAddress, string? UserAgent)
{
    public const int MaxIpLength = 45;

    public const int MaxUserAgentLength = 400;

    public string? SafeIp => Truncate(IpAddress, MaxIpLength);

    public string? SafeUserAgent => Truncate(UserAgent, MaxUserAgentLength);

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
