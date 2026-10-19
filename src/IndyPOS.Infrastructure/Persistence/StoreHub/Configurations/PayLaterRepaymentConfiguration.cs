using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

/// <summary>Append-only: no soft-delete column and no query filter — a repayment is never removed.</summary>
public class PayLaterRepaymentConfiguration : IEntityTypeConfiguration<PayLaterRepayment>
{
    public void Configure(EntityTypeBuilder<PayLaterRepayment> builder)
    {
        builder.ToTable("pay_later_repayment");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.PayLaterId).HasColumnName("pay_later_id").IsRequired();
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.BusinessDate).HasColumnName("business_date").IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.HasOne<PayLater>()
               .WithMany()
               .HasForeignKey(e => e.PayLaterId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.StoreId, e.BusinessDate });
    }
}
