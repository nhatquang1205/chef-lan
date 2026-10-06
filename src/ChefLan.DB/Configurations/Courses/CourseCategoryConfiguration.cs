using ChefLan.Domain.Courses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class CourseCategoryConfiguration : IEntityTypeConfiguration<CourseCategory>
{
    public void Configure(EntityTypeBuilder<CourseCategory> builder)
    {
        builder.ToTable("course_categories", ChefLanSchemas.Courses);

        builder.HasKey(cc => new { cc.CourseId, cc.CategoryId }).HasName("pk_course_categories");

        builder.HasOne(cc => cc.Course)
            .WithMany(c => c.CourseCategories)
            .HasForeignKey(cc => cc.CourseId)
            .HasConstraintName("fk_course_categories_courses_course_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cc => cc.Category)
            .WithMany(c => c.CourseCategories)
            .HasForeignKey(cc => cc.CategoryId)
            .HasConstraintName("fk_course_categories_categories_category_id")
            .OnDelete(DeleteBehavior.Cascade);

        // The reverse lookup: all courses in a category.
        builder.HasIndex(cc => new { cc.CategoryId, cc.CourseId })
            .HasDatabaseName("ix_course_categories_category_id_course_id");
    }
}
