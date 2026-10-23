using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// Store sync health: heartbeats, refusals and resend requests (spec 2026-10-23 sync hardening). Read through the
/// schema alone, like the mirrors, so every table and column carries a comment.
/// </summary>
internal static class SyncHealthModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureHeartbeats(modelBuilder);
        ConfigureRejections(modelBuilder);
        ConfigureResendRequests(modelBuilder);
    }

    private static void ConfigureHeartbeats(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<CloudStoreHeartbeat>(entity =>
        {
            entity.ToTable("StoreHeartbeats", t => t.HasComment(
                "Each store's latest heartbeat from StoreHub (every 2 minutes while it runs), one row per store, overwritten. No history. A store PC is usually off at night, so an old ReceivedAtUtc is not by itself a fault."));
            entity.HasKey(e => e.StoreId);
            entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store, from its token.");
            entity.Property(e => e.ReceivedAtUtc).HasComment("When the cloud received this heartbeat, UTC: the store's last-seen time.");
            entity.Property(e => e.StoreHubVersion).HasMaxLength(50).HasComment("The StoreHub version that sent it.");
            entity.Property(e => e.PendingCount).HasComment("Outbox events still waiting to reach the cloud.");
            entity.Property(e => e.FailedCount).HasComment("Outbox events the cloud refused, parked at the store until a resend.");
            entity.Property(e => e.OldestPendingCreatedAtUtc).HasComment("When the oldest waiting event was created, UTC; null when none is waiting.");
            entity.Property(e => e.FailedEventsJson).HasColumnType("jsonb").HasComment("Up to 50 of the oldest Failed events: [{eventId, eventType, failedAtUtc, reason}]; null when none.");
        });

    private static void ConfigureRejections(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<CloudSyncRejection>(entity =>
        {
            entity.ToTable("SyncRejections", t => t.HasComment(
                "Every refusal the cloud gave on /sync/events (400 malformed batch, 403 wrong store), one row per store and event. Resolved when the same event is later accepted; resolved rows stay as history."));
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StoreId, e.EventId }).IsUnique().HasFilter("\"EventId\" IS NOT NULL");
            entity.HasIndex(e => new { e.StoreId, e.ResolvedAtUtc });
            entity.Property(e => e.Id).ValueGeneratedNever().HasComment("This row's id.");
            entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store whose token sent the refused request.");
            entity.Property(e => e.EventId).HasComment("The refused outbox event; null when the batch named no event that could be read.");
            entity.Property(e => e.EventType).HasMaxLength(100).HasComment("The refused event's type, when it could be read.");
            entity.Property(e => e.StatusCode).HasComment("The HTTP status the cloud answered: 400 or 403.");
            entity.Property(e => e.Reason).HasMaxLength(1000).HasComment("The cloud's refusal code and detail.");
            entity.Property(e => e.FirstRejectedAtUtc).HasComment("When this event was first refused, UTC.");
            entity.Property(e => e.LastRejectedAtUtc).HasComment("When this event was last refused, UTC.");
            entity.Property(e => e.Count).HasComment("How many times this event was refused.");
            entity.Property(e => e.ResolvedAtUtc).HasComment("When the cloud later accepted this event, UTC; null while unresolved.");
        });

    private static void ConfigureResendRequests(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<CloudResendRequest>(entity =>
        {
            entity.ToTable("SyncResendRequests", t => t.HasComment(
                "An admin's requests that a store resend its Failed outbox events. The store picks a request up in a heartbeat reply and reports it done in its next heartbeat."));
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StoreId, e.CompletedAtUtc });
            entity.Property(e => e.Id).ValueGeneratedNever().HasComment("This request's id, which the store reports back.");
            entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store asked to resend.");
            entity.Property(e => e.EventId).HasComment("The one event to resend; null for every Failed event the store has.");
            entity.Property(e => e.RequestedAtUtc).HasComment("When the admin asked, UTC.");
            entity.Property(e => e.CompletedAtUtc).HasComment("When the store reported it done, UTC; null while open.");
            entity.Property(e => e.MovedEventIdsJson).HasColumnType("jsonb").HasComment("The events the store moved back to Pending: [guid].");
            entity.Property(e => e.IgnoredEventIdsJson).HasColumnType("jsonb").HasComment("Asked-for events the store did not have as Failed: [guid].");
        });
}
