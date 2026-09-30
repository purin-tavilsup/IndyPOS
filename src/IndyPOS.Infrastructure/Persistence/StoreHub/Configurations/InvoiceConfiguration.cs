using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.StoreId)
            .HasColumnName("store_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.Property(e => e.LastModifiedUtc)
            .HasColumnName("last_modified_utc")
            .IsRequired();

        builder.Property(e => e.LegacyInvoiceId)
            .HasColumnName("legacy_invoice_id");

        // The database assigns every bill number from one sequence, so two tills selling at once
        // can never clash and restored older binaries still get a number (the forward-only gate).
        builder.Property(e => e.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasDefaultValueSql($"nextval('{InvoiceNumberSequence.Name}')")
            .ValueGeneratedOnAdd()
            .IsRequired();

        // Defect 8, scoped per store for the same reason as Product's: GeneralHardware invoices run
        // 79..139,758 and MimyMart's 67,994..165,286, which overlap.
        builder.HasIndex(e => new { e.StoreId, e.LegacyInvoiceId })
            .IsUnique();

        // Per store, like the legacy id: legacy numbers overlap across stores.
        builder.HasIndex(e => new { e.StoreId, e.InvoiceNumber })
            .IsUnique();

        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.CreatedUtc);
    }
}
