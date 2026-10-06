using System.ComponentModel.DataAnnotations;

namespace ChefLan.Commons.Auth;

/// <summary>
/// Bound twice as named options, from Jwt:Internal and Jwt:Customer. The two signing keys
/// MUST differ — that is the defence that actually prevents a customer token from being
/// accepted as a staff token.
/// </summary>
public sealed class JwtOptions
{
    [Required]
    public string Issuer { get; set; } = "cheflan";

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>At least 32 bytes for HS256. Comes from user-secrets or an env var.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 1;

    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 30;
}
