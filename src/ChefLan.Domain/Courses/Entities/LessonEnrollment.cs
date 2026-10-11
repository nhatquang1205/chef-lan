using ChefLan.Domain.Common;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

/// <summary>
/// A customer's access to ONE lesson, bought on its own. Sits beside <see cref="Enrollment"/> (access to a
/// whole course, which includes every lesson): to watch a lesson's videos a customer needs either.
/// Same shape and the same rules as an enrollment — points at a Customer, not a User; never soft
/// deleted (revoking is a status change that keeps the grant and the money record); the expiry is
/// snapshotted from the course's access period at grant time ("same rule as the course").
/// </summary>
public class LessonEnrollment : AuditableEntity, IHasRowVersion
{
    public long CustomerId { get; set; }

    public long LessonId { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;

    public EnrollmentSource Source { get; set; } = EnrollmentSource.AdminGrant;

    public DateTime? AccessGrantedAt { get; set; }

    /// <summary>Null means lifetime. Snapshotted from the lesson's COURSE at grant time.</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>What was actually received — a snapshot, not a lookup of the lesson price.</summary>
    public decimal? AmountPaidVnd { get; set; }

    public string Currency { get; set; } = "VND";

    public string? PaymentNote { get; set; }

    public string? PaymentReference { get; set; }

    /// <summary>Which staff account granted this. An id only, no foreign key.</summary>
    public long? GrantedByStaffId { get; set; }

    public uint RowVersion { get; set; }

    public Customer? Customer { get; set; }

    public Lesson? Lesson { get; set; }

    public bool IsActiveAt(DateTime utcNow) =>
        Status == EnrollmentStatus.Active && (ExpiresAt is null || ExpiresAt > utcNow);
}
