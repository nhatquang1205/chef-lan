using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

/// <summary>
/// courses.lesson_enrollments — access to a single lesson, bought on its own. The twin of enrollments
/// (access to a whole course); a customer needs either one to watch a lesson's videos.
/// </summary>
public sealed class LessonEnrollmentConfiguration : IEntityTypeConfiguration<LessonEnrollment>
{
    public void Configure(EntityTypeBuilder<LessonEnrollment> builder)
    {
        builder.ToTable("lesson_enrollments", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_lesson_enrollments_status", "status IN (1, 2, 3, 4)");
            t.HasCheckConstraint("ck_lesson_enrollments_source", "source IN (1, 2, 3)");
            t.HasCheckConstraint(
                "ck_lesson_enrollments_amount_non_negative",
                "amount_paid_vnd IS NULL OR amount_paid_vnd >= 0");
            t.HasCheckConstraint(
                "ck_lesson_enrollments_paid_has_date",
                "is_paid = false OR paid_at IS NOT NULL");
            t.HasCheckConstraint(
                "ck_lesson_enrollments_active_has_grant_date",
                "status <> 2 OR access_granted_at IS NOT NULL");
        });

        builder.HasKey(e => e.Id).HasName("pk_lesson_enrollments");

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
            .HasComment("NULL means lifetime. Snapshotted from the lesson's course at grant time.");

        builder.Property(e => e.IsPaid).HasDefaultValue(false);

        builder.Property(e => e.AmountPaidVnd)
            .HasPrecision(12, 0)
            .HasComment("What was actually received — a snapshot, not the current lesson price");

        builder.Property(e => e.Currency)
            .HasColumnType("char(3)")
            .HasDefaultValue("VND");

        builder.Property(e => e.PaymentNote).HasMaxLength(500);

        builder.Property(e => e.PaymentReference).HasMaxLength(100);

        builder.Property(e => e.GrantedByStaffId)
            .HasComment("identity.users.id — by id only, no FK, so courses does not depend on identity");

        builder.Property(e => e.RowVersion).IsRowVersion();

        // One row per customer per lesson: "can this customer watch this lesson?" is a single-row lookup,
        // and a regrant (after a revoke or expiry) reuses the row.
        builder.HasIndex(e => new { e.CustomerId, e.LessonId })
            .HasDatabaseName("uq_lesson_enrollments_customer_id_lesson_id")
            .IsUnique();

        builder.HasIndex(e => new { e.CustomerId, e.Status })
            .HasDatabaseName("ix_lesson_enrollments_customer_id_status");

        builder.HasIndex(e => new { e.LessonId, e.Status })
            .HasDatabaseName("ix_lesson_enrollments_lesson_id_status");

        // RESTRICT on both: a customer or lesson that has been paid for is never deleted out from under
        // the grant. (Lessons are soft-deleted anyway.)
        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .HasConstraintName("fk_lesson_enrollments_customers_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Lesson)
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .HasConstraintName("fk_lesson_enrollments_lessons_lesson_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
