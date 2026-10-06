using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_lessons_video_provider", "video_provider IN (0, 1, 2, 3, 4)");
            t.HasCheckConstraint("ck_lessons_duration_non_negative", "duration_seconds >= 0");
            t.HasCheckConstraint("ck_lessons_sort_order_non_negative", "sort_order >= 0");
            t.HasCheckConstraint(
                "ck_lessons_published_has_video",
                "is_published = false OR video_provider = 0 OR video_url IS NOT NULL");
        });

        builder.HasKey(l => l.Id).HasName("pk_lessons");

        builder.Property(l => l.Id).UseIdentityByDefaultColumn();

        builder.Property(l => l.Slug).HasMaxLength(160).IsRequired();

        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();

        builder.Property(l => l.VideoProvider)
            .HasConversion<int>()
            .HasDefaultValue(VideoProvider.YouTube)
            .HasComment("0=None, 1=YouTube, 2=Vimeo, 3=Cdn, 4=Facebook");

        builder.Property(l => l.VideoUrl).HasMaxLength(1000);

        builder.Property(l => l.VideoExternalId)
            .HasMaxLength(100)
            .HasComment("Provider id, so the frontend can build an embed without parsing URLs");

        builder.Property(l => l.DurationSeconds).HasDefaultValue(0);

        builder.Property(l => l.IsFreePreview).HasDefaultValue(false);

        builder.Property(l => l.IsPublished).HasDefaultValue(true);

        builder.Property(l => l.ThumbnailUrl).HasMaxLength(1000);

        builder.Property(l => l.ResourcesUrl).HasMaxLength(1000);

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
