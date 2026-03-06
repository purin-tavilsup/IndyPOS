using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class PayLaterConfiguration : IEntityTypeConfiguration<PayLater>
{
    public void Configure(EntityTypeBuilder<PayLater> builder)
    {
        builder.ToTable("pay_later");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.PaymentId)
            .HasColumnName("payment_id")
            .IsRequired();

        builder.Property(e => e.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.PayLaterAmount)
            .HasColumnName("pay_later_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.PaidAmount)
            .HasColumnName("paid_amount")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(e => e.IsCompleted)
            .HasColumnName("is_completed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.Property(e => e.LastModifiedUtc)
            .HasColumnName("last_modified_utc")
            .IsRequired();

        // RemainingAmount is calculated, not stored
        builder.Ignore(e => e.RemainingAmount);

        builder.HasOne(e => e.Payment)
            .WithOne(p => p.PayLater)
            .HasForeignKey<PayLater>(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Invoice)
            .WithMany()
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.IsCompleted);
    }
}
