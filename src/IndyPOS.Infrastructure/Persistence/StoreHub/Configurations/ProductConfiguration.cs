using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.StoreId)
            .HasColumnName("store_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description");

        builder.Property(e => e.Manufacturer)
            .HasColumnName("manufacturer")
            .HasMaxLength(200);

        builder.Property(e => e.Brand)
            .HasColumnName("brand")
            .HasMaxLength(200);

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(100);

        builder.Property(e => e.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.GroupPrice)
            .HasColumnName("group_price")
            .HasPrecision(18, 2);

        builder.Property(e => e.GroupPriceQuantity)
            .HasColumnName("group_price_qty");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.Property(e => e.LastModifiedUtc)
            .HasColumnName("last_modified_utc")
            .IsRequired();

        // Unique barcode per store
        builder.HasIndex(e => new { e.StoreId, e.Barcode })
            .IsUnique();

        builder.HasIndex(e => e.StoreId);

        builder.HasIndex(e => e.IsActive);
    }
}
