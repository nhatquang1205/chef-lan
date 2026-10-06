namespace ChefLan.Domain.Identity.Entities;

public class UserRefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    /// <summary>SHA-256, base64. The raw token is never stored.</summary>
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedIp { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? RevokedAt { get; set; }

    /// <summary>Rotation chain, so a replayed token can be detected.</summary>
    public string? ReplacedByTokenHash { get; set; }

    public User? User { get; set; }
}
