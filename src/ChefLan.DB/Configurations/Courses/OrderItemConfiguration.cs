using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

/// <summary>courses.order_items — what an order is for. Titles and prices are snapshots.</summary>
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_order_items_kind", "kind IN (1, 2)");
            t.HasCheckConstraint(
                "ck_order_items_target",
                "(kind = 1 AND course_id IS NOT NULL AND lesson_id IS NULL) OR (kind = 2 AND lesson_id IS NOT NULL AND course_id IS NULL)");
            t.HasCheckConstraint(
                "ck_order_items_prices",
                "list_price_vnd >= 0 AND discount_vnd >= 0 AND unit_price_vnd = list_price_vnd - discount_vnd");
        });

        builder.HasKey(i => i.Id).HasName("pk_order_items");

        builder.Property(i => i.Id).UseIdentityByDefaultColumn();

        builder.Property(i => i.Kind)
            .HasConversion<int>()
            .HasComment("1=Course, 2=Lesson");

        builder.Property(i => i.Title).HasMaxLength(300).IsRequired();

        builder.Property(i => i.CourseSlug).HasMaxLength(200).IsRequired();

        builder.Property(i => i.ListPriceVnd).HasPrecision(12, 0);

        builder.Property(i => i.DiscountVnd).HasPrecision(12, 0).HasDefaultValue(0m);

        builder.Property(i => i.DiscountReason).HasMaxLength(300);

        builder.Property(i => i.UnitPriceVnd).HasPrecision(12, 0);

        builder.HasIndex(i => i.OrderId).HasDatabaseName("ix_order_items_order_id");

        builder.HasIndex(i => i.CourseId)
            .HasDatabaseName("ix_order_items_course_id")
            .HasFilter("course_id IS NOT NULL");

        builder.HasIndex(i => i.LessonId)
            .HasDatabaseName("ix_order_items_lesson_id")
            .HasFilter("lesson_id IS NOT NULL");

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(i => i.CourseId)
            .HasConstraintName("fk_order_items_courses_course_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(i => i.LessonId)
            .HasConstraintName("fk_order_items_lessons_lesson_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
