using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movement");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.StoreId)
            .HasColumnName("store_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(e => e.QuantityDelta)
            .HasColumnName("quantity_delta")
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ReferenceId)
            .HasColumnName("reference_id");

        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.HasOne(e => e.Product)
            .WithMany(p => p.InventoryMovements)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite index for stock queries: WHERE store_id AND product_id
        builder.HasIndex(e => new { e.StoreId, e.ProductId, e.CreatedUtc });
    }
}
