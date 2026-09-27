using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class DebtRepaymentConfiguration : IEntityTypeConfiguration<DebtRepayment>
{
    public void Configure(EntityTypeBuilder<DebtRepayment> builder)
    {
        builder.MapCashDrawerEntry("debt_repayment");
        builder.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
    }
}
