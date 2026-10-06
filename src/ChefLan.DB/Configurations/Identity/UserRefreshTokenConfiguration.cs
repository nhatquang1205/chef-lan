using ChefLan.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Identity;

public sealed class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("user_refresh_tokens", ChefLanSchemas.Identity);

        builder.HasKey(t => t.Id).HasName("pk_user_refresh_tokens");

        builder.Property(t => t.Id).UseIdentityByDefaultColumn();

        // SHA-256, base64. Never the raw token.
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
            .HasDatabaseName("uq_user_refresh_tokens_token_hash")
            .IsUnique();

        builder.HasIndex(t => new { t.UserId, t.ExpiresAt })
            .HasDatabaseName("ix_user_refresh_tokens_user_id_expires_at");
    }
}
