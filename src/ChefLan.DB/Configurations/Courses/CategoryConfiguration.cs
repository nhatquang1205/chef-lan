using ChefLan.Domain.Courses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", ChefLanSchemas.Courses);

        builder.HasKey(c => c.Id).HasName("pk_categories");

        builder.Property(c => c.Id).UseIdentityByDefaultColumn();

        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();

        builder.Property(c => c.Slug).HasMaxLength(140).IsRequired();

        builder.Property(c => c.ThumbnailUrl).HasMaxLength(1000);

        builder.Property(c => c.DisplayOrder).HasDefaultValue(0);

        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(c => c.Slug)
            .HasDatabaseName("uq_categories_slug")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
