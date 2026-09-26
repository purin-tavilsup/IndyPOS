using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class CashFloatConfiguration : IEntityTypeConfiguration<CashFloat>
{
    public void Configure(EntityTypeBuilder<CashFloat> builder)
    {
        builder.MapCashDrawerEntry("cash_float");
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
    }
}
