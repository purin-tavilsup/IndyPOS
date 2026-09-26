using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class CashPayoutConfiguration : IEntityTypeConfiguration<CashPayout>
{
    public void Configure(EntityTypeBuilder<CashPayout> builder)
    {
        builder.MapCashDrawerEntry("cash_payout");

        builder.Property(e => e.Category)
               .HasColumnName("category")
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(Domain.Enums.PayoutCategory.General)
               .IsRequired();

        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
    }
}
