namespace ChefLan.Domain.Customers.Entities;

public class CustomerRefreshToken
{
    public long Id { get; set; }

    public long CustomerId { get; set; }

    /// <summary>SHA-256, base64. The raw token is never stored.</summary>
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedIp { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public Customer? Customer { get; set; }
}
