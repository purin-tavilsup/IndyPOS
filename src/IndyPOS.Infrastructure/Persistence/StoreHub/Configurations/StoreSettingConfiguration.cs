using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class StoreSettingConfiguration : IEntityTypeConfiguration<StoreSetting>
{
    public void Configure(EntityTypeBuilder<StoreSetting> builder)
    {
        builder.ToTable("store_setting");

        // Composite key: unique setting per store
        builder.HasKey(e => new { e.StoreId, e.Key });

        builder.Property(e => e.StoreId)
               .HasColumnName("store_id")
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.Key)
               .HasColumnName("key")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.Value)
               .HasColumnName("value")
               .HasMaxLength(1000)
               .IsRequired();

        builder.Property(e => e.LastModifiedUtc)
               .HasColumnName("last_modified_utc")
               .IsRequired();

        // Index for store-specific lookups
        builder.HasIndex(e => e.StoreId);

        // Note: BarcodeCounter is seeded via DevelopmentDataSeeder or migration script
        // EF Core HasData requires compile-time constants, so we seed dynamically
    }
}
