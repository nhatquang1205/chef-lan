using ChefLan.Domain.Courses.Entities;
using ChefLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChefLan.DB.Configurations.Courses;

/// <summary>
/// courses.orders — a customer's request to buy, paid by bank transfer or arranged with chef Lan. Not soft-deletable: a money record.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", ChefLanSchemas.Courses, t =>
        {
            t.HasCheckConstraint("ck_orders_status", "status IN (1, 2, 3, 4)");
            t.HasCheckConstraint("ck_orders_payment_method", "payment_method IN (1, 2, 3)");
            t.HasCheckConstraint("ck_orders_paid_via", "paid_via IS NULL OR paid_via IN (1, 2)");
            t.HasCheckConstraint("ck_orders_amount_positive", "amount_vnd > 0");
            t.HasCheckConstraint("ck_orders_paid_has_date", "status <> 2 OR paid_at IS NOT NULL");
            t.HasCheckConstraint("ck_orders_manual_has_reason", "paid_via IS DISTINCT FROM 2 OR paid_reason IS NOT NULL");
        });

        builder.HasKey(o => o.Id).HasName("pk_orders");

        builder.Property(o => o.Id).UseIdentityByDefaultColumn();

        builder.Property(o => o.Code)
            .HasMaxLength(20)
            .IsRequired()
            .HasComment("Goes in the bank transfer note; how a transaction finds its order");

        builder.Property(o => o.Status)
            .HasConversion<int>()
            .HasDefaultValue(OrderStatus.Pending)
            .HasComment("1=Pending, 2=Paid, 3=Expired, 4=Cancelled. Expiry is lazy: Pending past expires_at reads as Expired");

        builder.Property(o => o.PaymentMethod)
            .HasConversion<int>()
            .HasDefaultValue(OrderPaymentMethod.BankTransfer)
            .HasComment("1=BankTransfer, 2=ContactChef, 3=StaffCreated");

        builder.Property(o => o.AmountVnd)
            .HasPrecision(12, 0)
            .HasComment("From the server-side quote when the order was created");

        builder.Property(o => o.DiscountVnd)
            .HasPrecision(12, 0)
            .HasComment("Staff's discount on the whole order (already split over the items); null when there is none");

        builder.Property(o => o.DiscountReason).HasMaxLength(300);

        builder.Property(o => o.PaidVia)
            .HasConversion<int?>()
            .HasComment("1=Webhook, 2=Manual");

        builder.Property(o => o.PaidReason).HasMaxLength(300);

        builder.Property(o => o.PaidByStaffId)
            .HasComment("identity.users.id — by id only, no FK, so courses does not depend on identity");

        builder.Property(o => o.PaymentTransactionId)
            .HasComment("payment_transactions.id of the bank transaction that paid it. No FK: the two tables point at each other");

        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasIndex(o => o.Code).HasDatabaseName("uq_orders_code").IsUnique();

        builder.HasIndex(o => new { o.CustomerId, o.Status })
            .HasDatabaseName("ix_orders_customer_id_status");

        builder.HasIndex(o => new { o.Status, o.CreatedAt })
            .HasDatabaseName("ix_orders_status_created_at");

        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .HasConstraintName("fk_orders_customers_customer_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .HasConstraintName("fk_order_items_orders_order_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
