using ChefLan.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Customers;

public sealed class CustomerRefreshTokenConfiguration : IEntityTypeConfiguration<CustomerRefreshToken>
{
    public void Configure(EntityTypeBuilder<CustomerRefreshToken> builder)
    {
        builder.ToTable("customer_refresh_tokens", ChefLanSchemas.Customers);

        builder.HasKey(t => t.Id).HasName("pk_customer_refresh_tokens");

        builder.Property(t => t.Id).UseIdentityByDefaultColumn();

        builder.Property(t => t.TokenHash)
            .HasMaxLength(88)
            .IsRequired();

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        // varchar, not inet: Npgsql maps inet to IPAddress, and this is a display-only
        // audit field. 45 characters covers IPv6.
        builder.Property(t => t.CreatedIp).HasMaxLength(45);

        builder.Property(t => t.UserAgent).HasMaxLength(400);

        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(88);

        builder.HasIndex(t => t.TokenHash)
            .HasDatabaseName("uq_customer_refresh_tokens_token_hash")
            .IsUnique();

        builder.HasIndex(t => new { t.CustomerId, t.ExpiresAt })
            .HasDatabaseName("ix_customer_refresh_tokens_customer_id_expires_at");
    }
}
