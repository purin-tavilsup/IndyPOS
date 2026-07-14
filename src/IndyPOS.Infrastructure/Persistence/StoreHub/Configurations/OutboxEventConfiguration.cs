using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_event");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.StoreId)
            .HasColumnName("store_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PayloadJson)
            .HasColumnName("payload_json")
            .IsRequired();

        builder.Property(e => e.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.Property(e => e.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.LastAttemptUtc)
            .HasColumnName("last_attempt_utc");

        builder.Property(e => e.NextRetryUtc)
            .HasColumnName("next_retry_utc");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("Pending")
            .IsRequired();

        // Index for SyncWorker polling: WHERE status = 'Pending' ORDER BY next_retry_utc, created_utc
        builder.HasIndex(e => new { e.Status, e.NextRetryUtc, e.CreatedUtc });
    }
}
