using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(e => e.Method)
            .HasColumnName("method")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Note)
            .HasColumnName("note");

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.Property(e => e.LegacyPaymentId)
            .HasColumnName("legacy_payment_id");

        // Defect 8, scoped to the invoice for the same reason as InvoiceLine's.
        builder.HasIndex(e => new { e.InvoiceId, e.LegacyPaymentId })
            .IsUnique();

        builder.HasOne(e => e.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
