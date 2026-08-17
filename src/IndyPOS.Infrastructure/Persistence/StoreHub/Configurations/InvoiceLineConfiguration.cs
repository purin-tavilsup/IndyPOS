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

        // Defect 6. All three NULLABLE, which is what lets this migration run against the previous
        // release's binaries -- the forward-only release gate in CLAUDE.md. Their INSERTs simply do
        // not mention these columns.
        // 200 not 50: the longest real note is 47 characters, and ProductName beside it is 200.
        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(200);

        builder.Property(e => e.Priority)
            .HasColumnName("priority");

        builder.Property(e => e.GroupPrice)
            .HasColumnName("group_price")
            .HasPrecision(18, 2);

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
