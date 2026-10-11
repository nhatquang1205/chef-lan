using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_categories_status", "status IN (1, 2)");
        });

        builder.HasKey(c => c.Id).HasName("pk_categories");

        builder.Property(c => c.Id).UseIdentityByDefaultColumn();

        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();

        builder.Property(c => c.Slug).HasMaxLength(140).IsRequired();

        builder.Property(c => c.ThumbnailUrl).HasMaxLength(1000);

        builder.Property(c => c.DisplayOrder).HasDefaultValue(0);

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .HasDefaultValue(CategoryStatus.Active)
            .HasComment("1=Active, 2=InActive");

        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        // Filtered on is_deleted ONLY, deliberately not on status: an InActive category
        // keeps owning its slug, otherwise reactivating it could collide with a newer one.
        builder.HasIndex(c => c.Slug)
            .HasDatabaseName("uq_categories_slug")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("ix_categories_status")
            .HasFilter("is_deleted = false");

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
