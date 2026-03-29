using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class StoreSettingConfiguration : IEntityTypeConfiguration<StoreSetting>
{
    public void Configure(EntityTypeBuilder<StoreSetting> builder)
    {
        builder.ToTable("store_setting");

        builder.HasKey(e => e.Key);

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

        // Note: BarcodeCounter is seeded via DevelopmentDataSeeder or migration script
        // EF Core HasData requires compile-time constants, so we seed dynamically
    }
}
