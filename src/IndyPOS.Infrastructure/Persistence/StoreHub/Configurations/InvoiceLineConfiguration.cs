using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_line");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(e => e.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(e => e.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        // LineTotal is calculated, not stored
        builder.Ignore(e => e.LineTotal);

        builder.HasOne(e => e.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.InvoiceLines)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
