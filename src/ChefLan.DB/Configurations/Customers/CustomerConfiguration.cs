using ChefLan.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Customers;

/// <summary>
/// customers.customers — EXTERNAL buyers. No foreign key to identity.users, in either
/// direction: the two populations are disjoint.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", ChefLanSchemas.Customers, t =>
        {
            // You cannot sign in without a login handle.
            t.HasCheckConstraint(
                "ck_customers_password_requires_email",
                "password_hash IS NULL OR email IS NOT NULL");

            t.HasCheckConstraint(
                "ck_customers_activated_requires_password",
                "activated_at IS NULL OR password_hash IS NOT NULL");
        });

        builder.HasKey(c => c.Id).HasName("pk_customers");

        builder.Property(c => c.Id).UseIdentityByDefaultColumn();

        // Nullable: a Facebook-era customer may have no email on record at all.
        builder.Property(c => c.Email).HasColumnType("citext");

        // Nullable: staff can record a customer who cannot sign in yet.
        builder.Property(c => c.PasswordHash).HasMaxLength(255);

        // SHA-256 of the one-time invitation token, as for refresh tokens. A new invite overwrites it.
        builder.Property(c => c.InviteTokenHash).HasMaxLength(64);

        builder.HasIndex(c => c.InviteTokenHash)
            .HasDatabaseName("uq_customers_invite_token_hash")
            .IsUnique()
            .HasFilter("invite_token_hash IS NOT NULL");

        builder.Property(c => c.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(20)
            .HasComment("E.164, e.g. +8490...");

        builder.Property(c => c.FacebookName).HasMaxLength(200);

        builder.Property(c => c.FacebookProfileUrl).HasMaxLength(500);

        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.Property(c => c.FailedLoginCount).HasDefaultValue(0);

        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        builder.Property(c => c.CreatedByStaffId)
            .HasComment("identity.users.id — by id only, no FK: it crosses a schema boundary");

        builder.Ignore(c => c.CanSignIn);

        // Partial on two conditions: many customers have no email, but an email that does
        // exist is the login handle and must be unique.
        builder.HasIndex(c => c.Email)
            .HasDatabaseName("uq_customers_email")
            .IsUnique()
            .HasFilter("email IS NOT NULL AND is_deleted = false");

        builder.HasIndex(c => c.Phone)
            .HasDatabaseName("ix_customers_phone")
            .HasFilter("is_deleted = false");

        builder.HasIndex(c => c.FullName)
            .HasDatabaseName("ix_customers_full_name")
            .HasFilter("is_deleted = false");

        builder.HasMany(c => c.RefreshTokens)
            .WithOne(t => t.Customer)
            .HasForeignKey(t => t.CustomerId)
            .HasConstraintName("fk_customer_refresh_tokens_customers_customer_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
