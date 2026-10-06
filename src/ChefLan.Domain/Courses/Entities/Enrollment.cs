using ChefLan.Domain.Common;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

/// <summary>
/// A customer's access to a course. Points at a Customer, not a User — so a Facebook-era
/// customer with no credentials can still hold an enrollment.
///
/// Not soft-deletable: revocation is a status change, which preserves the grant and the
/// money record.
/// </summary>
public class Enrollment : AuditableEntity, IHasRowVersion
{
    public long CustomerId { get; set; }

    public long CourseId { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;

    public EnrollmentSource Source { get; set; } = EnrollmentSource.AdminGrant;

    public DateTime? AccessGrantedAt { get; set; }

    /// <summary>
    /// Null means lifetime. Snapshotted from Course.AccessDurationDays at grant time, so
    /// editing the course later never retroactively changes existing students.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>What she actually received — a snapshot, not a lookup of the course price.</summary>
    public decimal? AmountPaidVnd { get; set; }

    public string Currency { get; set; } = "VND";

    public string? PaymentNote { get; set; }

    /// <summary>Bank transaction reference — the natural join key to a future payments table.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>Which staff account granted this. An id only, no foreign key.</summary>
    public long? GrantedByStaffId { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public uint RowVersion { get; set; }

    public Customer? Customer { get; set; }

    public Course? Course { get; set; }

    public bool IsActiveAt(DateTime utcNow) =>
        Status == EnrollmentStatus.Active && (ExpiresAt is null || ExpiresAt > utcNow);
}
