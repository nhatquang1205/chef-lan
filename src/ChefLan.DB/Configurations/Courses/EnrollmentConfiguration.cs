using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

/// <summary>
/// courses.enrollments — points at a CUSTOMER, not a staff user, so a Facebook-era
/// customer with no credentials can still hold an enrollment.
///
/// Not soft-deletable: revoking is a status change, which keeps the grant and the money
/// record intact.
/// </summary>
public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_enrollments_status", "status IN (1, 2, 3, 4)");
            t.HasCheckConstraint("ck_enrollments_source", "source IN (1, 2, 3)");
            t.HasCheckConstraint(
                "ck_enrollments_amount_non_negative",
                "amount_paid_vnd IS NULL OR amount_paid_vnd >= 0");
            t.HasCheckConstraint(
                "ck_enrollments_paid_has_date",
                "is_paid = false OR paid_at IS NOT NULL");
            t.HasCheckConstraint(
                "ck_enrollments_active_has_grant_date",
                "status <> 2 OR access_granted_at IS NOT NULL");
        });

        builder.HasKey(e => e.Id).HasName("pk_enrollments");

        builder.Property(e => e.Id).UseIdentityByDefaultColumn();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(EnrollmentStatus.Pending)
            .HasComment("1=Pending, 2=Active, 3=Expired, 4=Revoked");

        builder.Property(e => e.Source)
            .HasConversion<int>()
            .HasDefaultValue(EnrollmentSource.AdminGrant)
            .HasComment("1=AdminGrant, 2=FacebookMigration, 3=OnlineCheckout");

        builder.Property(e => e.ExpiresAt)
            .HasComment("NULL means lifetime. Snapshotted from the course at grant time.");

        builder.Property(e => e.IsPaid).HasDefaultValue(false);

        builder.Property(e => e.AmountPaidVnd)
            .HasPrecision(12, 0)
            .HasComment("What was actually received — a snapshot, not the current course price");

        builder.Property(e => e.Currency)
            .HasColumnType("char(3)")
            .HasDefaultValue("VND");

        builder.Property(e => e.PaymentNote).HasMaxLength(500);

        builder.Property(e => e.PaymentReference)
            .HasMaxLength(100)
            .HasComment("Bank transaction reference — the join key to a future payments table");

        builder.Property(e => e.GrantedByStaffId)
            .HasComment("identity.users.id — by id only, no FK, so courses does not depend on identity");

        builder.Property(e => e.RowVersion).IsRowVersion();

        // One row per customer per course: "does this customer have access?" is a
        // single-row lookup, and re-enrollment extends the existing row.
        builder.HasIndex(e => new { e.CustomerId, e.CourseId })
            .HasDatabaseName("uq_enrollments_customer_id_course_id")
            .IsUnique();

        builder.HasIndex(e => new { e.CustomerId, e.Status })
            .HasDatabaseName("ix_enrollments_customer_id_status");

        builder.HasIndex(e => new { e.CourseId, e.Status })
            .HasDatabaseName("ix_enrollments_course_id_status");

        // The expiry sweeper only ever looks at active rows.
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("ix_enrollments_expires_at")
            .HasFilter("status = 2");

        // Crosses the courses -> customers schema boundary, which is legal because one
        // context owns both. RESTRICT: a customer who holds enrollments is never deleted
        // out from under them.
        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .HasConstraintName("fk_enrollments_customers_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .HasConstraintName("fk_enrollments_courses_course_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
