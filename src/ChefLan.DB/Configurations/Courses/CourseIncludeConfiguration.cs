using ChefLan.Domain.Courses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class CourseIncludeConfiguration : IEntityTypeConfiguration<CourseInclude>
{
    public void Configure(EntityTypeBuilder<CourseInclude> builder)
    {
        builder.ToTable("course_includes", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_course_includes_not_self", "course_id <> included_course_id");
            t.HasCheckConstraint("ck_course_includes_discount_non_negative", "discount_vnd >= 0");
        });

        builder.HasKey(i => new { i.CourseId, i.IncludedCourseId }).HasName("pk_course_includes");

        builder.Property(i => i.DiscountVnd)
            .HasPrecision(12, 0)
            .HasDefaultValue(0m)
            .HasComment("Taken off the parent's price when the customer already has the included course");

        builder.HasOne(i => i.Course)
            .WithMany(c => c.Includes)
            .HasForeignKey(i => i.CourseId)
            .HasConstraintName("fk_course_includes_courses_course_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.IncludedCourse)
            .WithMany(c => c.IncludedIn)
            .HasForeignKey(i => i.IncludedCourseId)
            .HasConstraintName("fk_course_includes_courses_included_course_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Reverse lookup: which courses include C.
        builder.HasIndex(i => i.IncludedCourseId)
            .HasDatabaseName("ix_course_includes_included_course_id");
    }
}
