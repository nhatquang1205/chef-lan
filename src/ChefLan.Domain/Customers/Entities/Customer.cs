using ChefLan.Domain.Common;

namespace ChefLan.Domain.Customers.Entities;

/// <summary>
/// An external buyer. Entirely separate from <see cref="Identity.Entities.User"/>:
/// different table, different schema, different JWT scheme, no foreign key either way.
///
/// Email and PasswordHash are both nullable: staff can record a Facebook-era customer
/// who has no account yet and grant them enrollments immediately. They become able to
/// log in once an invite is accepted and a password is set.
/// </summary>
public class Customer : AuditableSoftDeletableEntity
{
    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public required string FullName { get; set; }

    public string? Phone { get; set; }

    /// <summary>The display name chef Lan sees in the Facebook group.</summary>
    public string? FacebookName { get; set; }

    public string? FacebookProfileUrl { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? EmailConfirmedAt { get; set; }

    /// <summary>When they first set a password.</summary>
    public DateTime? ActivatedAt { get; set; }

    public DateTime? InvitedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Which staff account entered this row. An id only, with no foreign key — it crosses
    /// the identity/customers boundary.
    /// </summary>
    public long? CreatedByStaffId { get; set; }

    public ICollection<CustomerRefreshToken> RefreshTokens { get; set; } = [];

    public bool CanSignIn => PasswordHash is not null && Email is not null && IsActive && !IsDeleted;
}
