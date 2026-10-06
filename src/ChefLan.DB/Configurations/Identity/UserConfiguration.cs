using ChefLan.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Identity;

/// <summary>
/// identity.users — INTERNAL staff accounts only. No customer columns belong here.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", ChefLanSchemas.Identity, t =>
        {
            t.HasCheckConstraint("ck_users_role", "role IN (1, 2)");
        });

        builder.HasKey(u => u.Id).HasName("pk_users");

        builder.Property(u => u.Id).UseIdentityByDefaultColumn();

        builder.Property(u => u.Email)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.UserRole.Staff)
            .HasComment("1=Admin, 2=Staff");

        builder.Property(u => u.JobTitle).HasMaxLength(120);

        builder.Property(u => u.IsActive).HasDefaultValue(true);

        builder.Property(u => u.MustChangePassword).HasDefaultValue(false);

        builder.Property(u => u.FailedLoginCount).HasDefaultValue(0);

        builder.Property(u => u.IsDeleted).HasDefaultValue(false);

        // Partial, because soft-deleted rows keep their email. Without the filter, an
        // address could never be reused after a staff member is removed.
        builder.HasIndex(u => u.Email)
            .HasDatabaseName("uq_users_email")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("ix_users_role")
            .HasFilter("is_deleted = false");

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .HasConstraintName("fk_user_refresh_tokens_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
