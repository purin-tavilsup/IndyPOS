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

        // Defect 8, scoped per store for the same reason as Product's: GeneralHardware invoices run
        // 79..139,758 and MimyMart's 67,994..165,286, which overlap.
        builder.HasIndex(e => new { e.StoreId, e.LegacyInvoiceId })
            .IsUnique();

        builder.HasIndex(e => e.StoreId);
        builder.HasIndex(e => e.CreatedUtc);
    }
}
