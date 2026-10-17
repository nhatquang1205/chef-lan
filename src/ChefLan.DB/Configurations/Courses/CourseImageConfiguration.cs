using ChefLan.Domain.Courses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class CourseImageConfiguration : IEntityTypeConfiguration<CourseImage>
{
    public void Configure(EntityTypeBuilder<CourseImage> builder)
    {
        builder.ToTable("course_images", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_course_images_sort_order_non_negative", "sort_order >= 0");
            t.HasCheckConstraint("ck_course_images_width_positive", "width IS NULL OR width > 0");
            t.HasCheckConstraint("ck_course_images_height_positive", "height IS NULL OR height > 0");
        });

        builder.HasKey(i => i.Id).HasName("pk_course_images");

        builder.Property(i => i.Id).UseIdentityByDefaultColumn();

        builder.Property(i => i.Url).HasMaxLength(1000).IsRequired();

        builder.Property(i => i.Alt).HasMaxLength(200);

        // DELIBERATELY NOT UNIQUE — same reason as lessons and lesson videos: a unique index would reject the
        // intermediate states of a reorder. The reorder use case rewrites the course's rows in one transaction.
        builder.HasIndex(i => new { i.CourseId, i.SortOrder })
            .HasDatabaseName("ix_course_images_course_id_sort_order");

        builder.HasOne(i => i.Course)
            .WithMany(c => c.Images)
            .HasForeignKey(i => i.CourseId)
            .HasConstraintName("fk_course_images_courses_course_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
