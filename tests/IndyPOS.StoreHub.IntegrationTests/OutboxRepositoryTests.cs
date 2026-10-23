using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// A refused event used to stay Pending and be picked up again on every poll, so ten of them filled every
/// batch and nothing newer was ever sent. Refused events are now parked as Failed, with the cloud's
/// reason. The shared database is not reset between tests, so these assert on their own event ids only.
/// </summary>
[Collection("Integration")]
public class OutboxRepositoryTests : IntegrationTestBase
{
    private const int EveryPendingEvent = 10_000;

    public OutboxRepositoryTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private OutboxRepository CreateRepository() => new(GetDbContext());

    private async Task<OutboxEvent> AddPendingEventAsync()
    {
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            StoreId = "test-store",
            Type = "InvoiceCompleted",
            PayloadJson = """{"StoreId":"test-store"}""",
            CreatedUtc = DateTime.UtcNow,
            Status = "Pending"
        };

        await using var db = GetDbContext();
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync();

        return outboxEvent;
    }

    private async Task<OutboxEvent> ReloadAsync(Guid eventId)
    {
        await using var db = GetDbContext();

        return await db.OutboxEvents.AsNoTracking()
                                    .SingleAsync(e => e.Id == eventId);
    }

    [Fact]
    public async Task GetPendingEventsAsync_WithRefusedEvents_DoesNotReturnThem()
    {
        var refusedIds = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            var refused = await AddPendingEventAsync();
            await CreateRepository().MarkAsRefusedAsync(refused.Id, "403 store-mismatch");
            refusedIds.Add(refused.Id);
        }
        var pending = await AddPendingEventAsync();

        var due = await CreateRepository().GetPendingEventsAsync(EveryPendingEvent);

        due.Select(e => e.Id).Should()
                             .Contain(pending.Id)
                             .And.NotContain(refusedIds);
    }

    [Fact]
    public async Task MarkAsRefusedAsync_WithAReason_StoresItAsFailed()
    {
        var outboxEvent = await AddPendingEventAsync();

        await CreateRepository().MarkAsRefusedAsync(outboxEvent.Id, "403 store-mismatch: not this store");

        var stored = await ReloadAsync(outboxEvent.Id);
        stored.Status.Should()
                     .Be("Failed");
        stored.LastError.Should()
                        .Be("403 store-mismatch: not this store");
        stored.NextRetryUtc.Should()
                           .BeNull();
        stored.Attempts.Should()
                       .Be(1);
    }

    [Fact]
    public async Task MarkAsRefusedAsync_WithAReasonLongerThanTheColumn_StoresItTruncated()
    {
        var outboxEvent = await AddPendingEventAsync();

        await CreateRepository().MarkAsRefusedAsync(outboxEvent.Id, new string('x', 1_500));

        var stored = await ReloadAsync(outboxEvent.Id);
        stored.LastError!.Length.Should()
                                .Be(OutboxEvent.LastErrorMaxLength);
    }

    // Cutting at the column's length could split a two-unit character and leave half of it, which
    // PostgreSQL's UTF-8 encoding rejects: the write would throw and the event would be retried with
    // no backoff on every poll.
    [Fact]
    public async Task MarkAsRefusedAsync_WithACharacterSplitAtTheColumnLength_StoresItAsFailed()
    {
        var outboxEvent = await AddPendingEventAsync();
        var reason = new string('x', OutboxEvent.LastErrorMaxLength - 1) + "😀" + "tail";

        await CreateRepository().MarkAsRefusedAsync(outboxEvent.Id, reason);

        (await ReloadAsync(outboxEvent.Id)).Status.Should()
                                               .Be("Failed");
    }

    [Fact]
    public async Task ScheduleRetryAsync_WithAReason_KeepsTheEventPending()
    {
        var outboxEvent = await AddPendingEventAsync();
        var nextRetryUtc = DateTime.UtcNow.AddMinutes(15);

        await CreateRepository().ScheduleRetryAsync(outboxEvent.Id, nextRetryUtc, "503 from the cloud");

        var stored = await ReloadAsync(outboxEvent.Id);
        stored.Status.Should()
                     .Be("Pending");
        stored.NextRetryUtc.Should()
                           .BeCloseTo(nextRetryUtc, TimeSpan.FromMilliseconds(1));
        stored.LastError.Should()
                        .Be("503 from the cloud");
    }

    [Fact]
    public async Task GetFailedCountAsync_WithARetriedPendingEvent_DoesNotCountIt()
    {
        var outboxEvent = await AddPendingEventAsync();
        var before = await CreateRepository().GetFailedCountAsync();

        await CreateRepository().ScheduleRetryAsync(outboxEvent.Id, DateTime.UtcNow.AddMinutes(1), "timeout");

        (await CreateRepository().GetFailedCountAsync()).Should()
                                                        .Be(before);
    }

    [Fact]
    public async Task GetFailedCountAsync_WithARefusedEvent_CountsIt()
    {
        var outboxEvent = await AddPendingEventAsync();
        var before = await CreateRepository().GetFailedCountAsync();

        await CreateRepository().MarkAsRefusedAsync(outboxEvent.Id, "400 malformed-batch");

        (await CreateRepository().GetFailedCountAsync()).Should()
                                                        .Be(before + 1);
    }
}
