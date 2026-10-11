using ChefLan.Domain.Courses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_lessons_duration_non_negative", "duration_seconds >= 0");
            t.HasCheckConstraint("ck_lessons_sort_order_non_negative", "sort_order >= 0");
            t.HasCheckConstraint("ck_lessons_video_count_non_negative", "video_count >= 0");
            t.HasCheckConstraint("ck_lessons_price_non_negative", "price_vnd IS NULL OR price_vnd >= 0");

            // ck_lessons_video_provider and ck_lessons_published_has_video are gone — the
            // columns they guarded moved to lesson_videos, and the equivalent rule is
            // ck_lesson_videos_published_has_source.
        });

        builder.HasKey(l => l.Id).HasName("pk_lessons");

        builder.Property(l => l.Id).UseIdentityByDefaultColumn();

        builder.Property(l => l.Slug).HasMaxLength(160).IsRequired();

        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();

        builder.Property(l => l.Recipes).HasComment("Markdown");

        builder.Property(l => l.IsPublished).HasDefaultValue(true);

        builder.Property(l => l.ResourcesUrl).HasMaxLength(1000);

        builder.Property(l => l.PriceVnd)
            .HasPrecision(12, 0)
            .HasComment("Price to buy this lesson alone. NULL = not sold separately, only through the course");

        builder.Property(l => l.VideoCount)
            .HasDefaultValue(0)
            .HasComment("Denormalized count of published, non-deleted lesson_videos");

        builder.Property(l => l.DurationSeconds)
            .HasDefaultValue(0)
            .HasComment("Denormalized sum of published, non-deleted lesson_videos durations");

        builder.Property(l => l.IsDeleted).HasDefaultValue(false);

        builder.Property(l => l.RowVersion).IsRowVersion();

        builder.HasIndex(l => new { l.CourseId, l.Slug })
            .HasDatabaseName("uq_lessons_course_slug")
            .IsUnique()
            .HasFilter("is_deleted = false");

        // DELIBERATELY NOT UNIQUE. A partial unique index cannot be DEFERRABLE in Postgres,
        // so any intermediate state during a drag-and-drop reorder would violate it.
        // Uniqueness of sort_order within a course is owned by the reorder use case, which
        // rewrites every row of the course in one transaction.
        builder.HasIndex(l => new { l.CourseId, l.SortOrder })
            .HasDatabaseName("ix_lessons_course_id_sort_order")
            .HasFilter("is_deleted = false");

        builder.HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .HasConstraintName("fk_lessons_courses_course_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
