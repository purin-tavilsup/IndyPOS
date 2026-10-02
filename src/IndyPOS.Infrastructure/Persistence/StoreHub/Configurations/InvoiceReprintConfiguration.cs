using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class InvoiceReprintConfiguration : IEntityTypeConfiguration<InvoiceReprint>
{
    public void Configure(EntityTypeBuilder<InvoiceReprint> builder)
    {
        builder.ToTable("invoice_reprint");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        // Restrict: an audit row must never vanish with its bill.
        builder.HasOne<Invoice>()
               .WithMany()
               .HasForeignKey(e => e.InvoiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.InvoiceId);
        builder.HasIndex(e => new { e.StoreId, e.CreatedUtc });
    }
}
