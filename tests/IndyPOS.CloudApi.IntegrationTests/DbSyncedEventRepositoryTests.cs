using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// MarkProcessedAsync used FindAsync with the event's Guid EventId, but the inbox key is a long Id.
/// EF threw on the type mismatch every time, so no event was ever marked processed and the
/// processor re-read the same oldest batch forever.
/// </summary>
public class DbSyncedEventRepositoryTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    [Fact]
    public async Task MarkProcessedAsync_WithAnUnknownEventId_DoesNotThrow()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());

        var act = () => new DbSyncedEventRepository(db).MarkProcessedAsync(Guid.NewGuid());

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task MarkProcessedAsync_WithAStoredEvent_SetsItsProcessedTime()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var stored = await AddEventAsync(db);

        await new DbSyncedEventRepository(db).MarkProcessedAsync(stored.EventId);

        (await ReadProcessedAtAsync(db, stored.EventId)).Should()
                                                        .NotBeNull();
    }

    [Fact]
    public async Task MarkProcessedAsync_WithAStoredEvent_LeavesOtherEventsUnprocessed()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var marked = await AddEventAsync(db);
        var other = await AddEventAsync(db);

        await new DbSyncedEventRepository(db).MarkProcessedAsync(marked.EventId);

        (await ReadProcessedAtAsync(db, other.EventId)).Should()
                                                       .BeNull();
    }

    [Fact]
    public async Task GetUnprocessedAsync_WithAnEventNotYetDueForRetry_SkipsIt()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var waiting = await AddEventAsync(db);
        await new DbSyncedEventRepository(db).MarkFailedAsync(waiting.EventId, DateTime.UtcNow.AddHours(1));

        var batch = await new DbSyncedEventRepository(db).GetUnprocessedAsync(["InvoiceCompleted"]);

        batch.Should()
             .BeEmpty();
    }

    [Fact]
    public async Task GetUnprocessedAsync_WithAnEventWhoseRetryIsDue_ReturnsIt()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var due = await AddEventAsync(db);
        await new DbSyncedEventRepository(db).MarkFailedAsync(due.EventId, DateTime.UtcNow.AddSeconds(-1));

        var batch = await new DbSyncedEventRepository(db).GetUnprocessedAsync(["InvoiceCompleted"]);

        batch.Select(e => e.EventId).Should()
                                    .Equal(due.EventId);
    }

    [Fact]
    public async Task MarkFailedAsync_WithAStoredEvent_CountsTheAttempt()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var failed = await AddEventAsync(db);

        await new DbSyncedEventRepository(db).MarkFailedAsync(failed.EventId, DateTime.UtcNow);
        await new DbSyncedEventRepository(db).MarkFailedAsync(failed.EventId, DateTime.UtcNow);

        (await db.SyncedEvents.AsNoTracking().SingleAsync(e => e.EventId == failed.EventId)).Attempts.Should()
                                                                                                    .Be(2);
    }

    private static async Task<SyncedEventEntity> AddEventAsync(CloudDbContext db)
    {
        var entity = new SyncedEventEntity
        {
            EventId = Guid.NewGuid(),
            StoreId = 1,
            EventType = "InvoiceCompleted",
            Payload = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow
        };
        db.SyncedEvents.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }

    private static Task<DateTime?> ReadProcessedAtAsync(CloudDbContext db, Guid eventId) =>
        db.SyncedEvents
          .AsNoTracking()
          .Where(e => e.EventId == eventId)
          .Select(e => e.ProcessedAtUtc)
          .SingleAsync();
}
