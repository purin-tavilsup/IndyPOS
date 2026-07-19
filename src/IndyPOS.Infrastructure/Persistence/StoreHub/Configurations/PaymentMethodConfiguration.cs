using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_method");
        builder.HasKey(e => new { e.StoreId, e.Code });

        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();
        builder.Property(e => e.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(e => e.DisplayOrder).HasColumnName("display_order").IsRequired();
        builder.Property(e => e.ValidFrom).HasColumnName("valid_from");
        builder.Property(e => e.ValidTo).HasColumnName("valid_to");
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
    }
}
