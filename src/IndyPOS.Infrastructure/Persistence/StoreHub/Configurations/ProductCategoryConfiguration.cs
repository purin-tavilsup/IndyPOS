using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_category");
        builder.HasKey(e => new { e.StoreId, e.Code });

        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(e => e.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
    }
}
