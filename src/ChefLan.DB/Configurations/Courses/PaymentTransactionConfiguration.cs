using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

/// <summary>
/// courses.payment_transactions — every bank transaction a provider reported, matched or not.
/// (provider, provider_transaction_id) is unique: the idempotency key for retried notifications.
/// </summary>
public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_payment_transactions_match_status", "match_status IN (1, 2, 3, 4, 5, 6, 7)");
            t.HasCheckConstraint("ck_payment_transactions_amount_non_negative", "amount_vnd >= 0");
        });

        builder.HasKey(p => p.Id).HasName("pk_payment_transactions");

        builder.Property(p => p.Id).UseIdentityByDefaultColumn();

        builder.Property(p => p.Provider).HasMaxLength(30).IsRequired();

        builder.Property(p => p.ProviderTransactionId).HasMaxLength(100).IsRequired();

        builder.Property(p => p.AmountVnd).HasPrecision(12, 0);

        builder.Property(p => p.Content).HasMaxLength(1000);

        builder.Property(p => p.Code).HasMaxLength(20);

        builder.Property(p => p.AccountNumber).HasMaxLength(50);

        builder.Property(p => p.RawPayload)
            .HasColumnType("text")
            .HasComment("The notification body exactly as received. Not jsonb: it may not be valid JSON");

        builder.Property(p => p.MatchStatus)
            .HasConversion<int>()
            .HasDefaultValue(PaymentMatchStatus.Unmatched)
            .HasComment("1=Unmatched, 2=Matched, 3=Overpaid, 4=Underpaid, 5=LateAfterExpiry, 6=Duplicate, 7=Ignored");

        builder.Property(p => p.Note).HasMaxLength(500);

        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => new { p.Provider, p.ProviderTransactionId })
            .HasDatabaseName("uq_payment_transactions_provider_provider_transaction_id")
            .IsUnique();

        builder.HasIndex(p => new { p.MatchStatus, p.CreatedAt })
            .HasDatabaseName("ix_payment_transactions_match_status_created_at");

        builder.HasIndex(p => p.OrderId)
            .HasDatabaseName("ix_payment_transactions_order_id")
            .HasFilter("order_id IS NOT NULL");

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .HasConstraintName("fk_payment_transactions_orders_order_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
