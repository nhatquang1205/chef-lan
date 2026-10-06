using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_courses_status", "status IN (1, 2, 3)");
            t.HasCheckConstraint("ck_courses_level", "level IN (0, 1, 2, 3)");
            t.HasCheckConstraint("ck_courses_price_non_negative", "price_vnd >= 0");
            t.HasCheckConstraint(
                "ck_courses_compare_at_price",
                "compare_at_price_vnd IS NULL OR compare_at_price_vnd >= price_vnd");
            t.HasCheckConstraint(
                "ck_courses_access_duration_positive",
                "access_duration_days IS NULL OR access_duration_days > 0");
            t.HasCheckConstraint(
                "ck_courses_published_has_date",
                "status <> 2 OR published_at IS NOT NULL");
        });

        builder.HasKey(c => c.Id).HasName("pk_courses");

        builder.Property(c => c.Id).UseIdentityByDefaultColumn();

        builder.Property(c => c.Slug).HasMaxLength(160).IsRequired();

        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();

        builder.Property(c => c.Subtitle).HasMaxLength(300);

        builder.Property(c => c.MetaDescription).HasMaxLength(300);

        builder.Property(c => c.ThumbnailUrl).HasMaxLength(1000);

        builder.Property(c => c.PromoVideoUrl).HasMaxLength(1000);

        builder.Property(c => c.Level)
            .HasConversion<int>()
            .HasDefaultValue(CourseLevel.Unspecified)
            .HasComment("0=Unspecified, 1=Beginner, 2=Intermediate, 3=Advanced");

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .HasDefaultValue(CourseStatus.Draft)
            .HasComment("1=Draft, 2=Published, 3=Archived");

        // numeric(12,0): VND has no minor unit, so a fractional price is always a bug.
        builder.Property(c => c.PriceVnd)
            .HasPrecision(12, 0)
            .HasDefaultValue(0m);

        builder.Property(c => c.CompareAtPriceVnd).HasPrecision(12, 0);

        builder.Property(c => c.Currency)
            .HasColumnType("char(3)")
            .HasDefaultValue("VND");

        builder.Property(c => c.AccessDurationDays)
            .HasComment("NULL means lifetime access");

        builder.Property(c => c.LessonCount).HasDefaultValue(0);

        builder.Property(c => c.TotalDurationSeconds).HasDefaultValue(0);

        builder.Property(c => c.EnrollmentCount).HasDefaultValue(0);

        builder.Property(c => c.LegacyFacebookGroupUrl)
            .HasMaxLength(1000)
            .HasComment("Bridge to the Facebook group this course used to be sold through");

        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        // xmin — optimistic concurrency at no storage cost.
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.HasIndex(c => c.Slug)
            .HasDatabaseName("uq_courses_slug")
            .IsUnique()
            .HasFilter("is_deleted = false");

        // The public catalog query.
        builder.HasIndex(c => new { c.Status, c.PublishedAt })
            .HasDatabaseName("ix_courses_status_published_at")
            .IsDescending(false, true)
            .HasFilter("is_deleted = false");

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
