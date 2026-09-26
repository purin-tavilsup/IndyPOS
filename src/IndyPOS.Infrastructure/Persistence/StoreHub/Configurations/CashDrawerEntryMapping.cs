using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

/// <summary>Column mapping, soft-delete filter and day index shared by the three entry tables.</summary>
internal static class CashDrawerEntryMapping
{
    public static void MapCashDrawerEntry<TEntry>(this EntityTypeBuilder<TEntry> builder, string tableName)
        where TEntry : CashDrawerEntry
    {
        builder.ToTable(tableName);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.BusinessDate).HasColumnName("business_date").IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(e => e.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.DeletedUtc).HasColumnName("deleted_utc");

        builder.HasQueryFilter(CashDrawerQueryFilters.SoftDelete, e => !e.IsDeleted);
        builder.HasIndex(e => new { e.StoreId, e.BusinessDate });
    }
}
