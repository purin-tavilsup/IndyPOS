using System.Collections.Concurrent;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.Cloud.Sync;

public class IngestEventsCommandHandlerTests
{
    private readonly FakeSyncedEventRepository _repository;
    private readonly IngestEventsCommandHandler _handler;

    public IngestEventsCommandHandlerTests()
    {
        _repository = new FakeSyncedEventRepository();
        _handler = new IngestEventsCommandHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_NewEvent_ShouldAcceptAndStore()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var events = new List<SyncEventRequest>
        {
            new(eventId, StoreId: 1, EventType: "InvoiceCompleted", Payload: "{}", CreatedAtUtc: DateTime.UtcNow)
        };
        var command = new IngestEventsCommand(events);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(_repository.Events.ContainsKey(eventId));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_ShouldReturnDuplicateAndNotStoreAgain()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _repository.Events[eventId] = new SyncedEventEntity { EventId = eventId };

        var events = new List<SyncEventRequest>
        {
            new(eventId, StoreId: 1, EventType: "InvoiceCompleted", Payload: "{}", CreatedAtUtc: DateTime.UtcNow)
        };
        var command = new IngestEventsCommand(events);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(0, result.AcceptedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(result.Results);
        Assert.True(result.Results[0].Accepted);
        Assert.Equal("duplicate", result.Results[0].Reason);
    }

    [Fact]
    public async Task HandleAsync_MixedEvents_ShouldHandleCorrectly()
    {
        // Arrange
        var existingEventId = Guid.NewGuid();
        var newEventId1 = Guid.NewGuid();
        var newEventId2 = Guid.NewGuid();

        _repository.Events[existingEventId] = new SyncedEventEntity { EventId = existingEventId };

        var events = new List<SyncEventRequest>
        {
            new(existingEventId, StoreId: 1, EventType: "InvoiceCompleted", Payload: "{}", CreatedAtUtc: DateTime.UtcNow),
            new(newEventId1, StoreId: 1, EventType: "InvoiceCompleted", Payload: "{}", CreatedAtUtc: DateTime.UtcNow),
            new(newEventId2, StoreId: 2, EventType: "InventoryMovementRecorded", Payload: "{}", CreatedAtUtc: DateTime.UtcNow)
        };
        var command = new IngestEventsCommand(events);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(2, result.AcceptedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(3, result.Results.Count);
    }

    [Fact]
    public async Task HandleAsync_EmptyBatch_ShouldReturnZeroCounts()
    {
        // Arrange
        var command = new IngestEventsCommand(new List<SyncEventRequest>());

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(0, result.AcceptedCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
    }

    private class FakeSyncedEventRepository : ISyncedEventRepository
    {
        public ConcurrentDictionary<Guid, SyncedEventEntity> Events { get; } = new();

        public Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken = default)
            => Task.FromResult(Events.ContainsKey(eventId));

        public Task AddAsync(SyncedEventEntity entity, CancellationToken cancellationToken = default)
        {
            Events.TryAdd(entity.EventId, entity);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SyncedEventEntity>> GetUnprocessedAsync(int limit = 100, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SyncedEventEntity>>(Events.Values.Where(e => e.ProcessedAtUtc == null).ToList());

        public Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            if (Events.TryGetValue(eventId, out var entity))
                entity.ProcessedAtUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }
}
