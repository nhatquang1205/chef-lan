using ChefLan.Domain.Common;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Identity.Entities;

/// <summary>
/// An INTERNAL staff account: chef Lan herself, and the assistants she hires.
/// Customers are a separate population in ChefLan.Domain.Customers — there is no
/// relationship between the two, by design.
/// </summary>
public class User : AuditableSoftDeletableEntity
{
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string DisplayName { get; set; }

    public UserRole Role { get; set; } = UserRole.Staff;

    public string? JobTitle { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>An Admin creates the account with a temporary password.</summary>
    public bool MustChangePassword { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = [];
}
