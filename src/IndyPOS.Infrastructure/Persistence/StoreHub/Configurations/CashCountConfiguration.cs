using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

/// <summary>Append-only: no soft-delete column and no query filter — counts are never removed.</summary>
public class CashCountConfiguration : IEntityTypeConfiguration<CashCount>
{
    public void Configure(EntityTypeBuilder<CashCount> builder)
    {
        builder.ToTable("cash_count");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.BusinessDate).HasColumnName("business_date").IsRequired();
        builder.Property(e => e.BankNote1000Count).HasColumnName("bank_note_1000_count").IsRequired();
        builder.Property(e => e.BankNote500Count).HasColumnName("bank_note_500_count").IsRequired();
        builder.Property(e => e.BankNote100Count).HasColumnName("bank_note_100_count").IsRequired();
        builder.Property(e => e.BankNote50Count).HasColumnName("bank_note_50_count").IsRequired();
        builder.Property(e => e.BankNote20Count).HasColumnName("bank_note_20_count").IsRequired();
        builder.Property(e => e.Coin10Count).HasColumnName("coin_10_count").IsRequired();
        builder.Property(e => e.Coin5Count).HasColumnName("coin_5_count").IsRequired();
        builder.Property(e => e.Coin2Count).HasColumnName("coin_2_count").IsRequired();
        builder.Property(e => e.Coin1Count).HasColumnName("coin_1_count").IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Ignore(e => e.CountedTotal);
        builder.HasIndex(e => new { e.StoreId, e.BusinessDate, e.CreatedUtc });
    }
}
