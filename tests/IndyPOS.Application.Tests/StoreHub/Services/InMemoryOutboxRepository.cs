using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>
/// An outbox that picks events by the same rules as OutboxRepository's query: Pending, due, oldest first,
/// at most a batch. A mock answers whatever it was told; this one lets a stuck event keep its place at the
/// head of the queue, which is the whole stall.
/// </summary>
internal sealed class InMemoryOutboxRepository : IOutboxRepository
{
    private readonly object _gate = new();
    private readonly List<OutboxEvent> _events = [];

    public void Add(OutboxEvent outboxEvent)
    {
        lock (_gate)
            _events.Add(outboxEvent);
    }

    public OutboxEvent Get(Guid eventId)
    {
        lock (_gate)
            return _events.Single(e => e.Id == eventId);
    }

    public Task<IReadOnlyList<OutboxEvent>> GetPendingEventsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        lock (_gate)
        {
            IReadOnlyList<OutboxEvent> due = _events.Where(e => e.Status == "Pending" && (e.NextRetryUtc == null || e.NextRetryUtc <= now))
                                                    .OrderBy(e => e.CreatedUtc)
                                                    .Take(batchSize)
                                                    .ToList();
            return Task.FromResult(due);
        }
    }

    public Task MarkAsSentAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        Update(eventId, e => e.Status = "Sent");

    public Task ScheduleRetryAsync(Guid eventId, DateTime nextRetryUtc, string? reason, CancellationToken cancellationToken = default) =>
        Update(eventId, e =>
        {
            e.Attempts++;
            e.NextRetryUtc = nextRetryUtc;
            e.LastError = reason;
        });

    public Task MarkAsRefusedAsync(Guid eventId, string reason, CancellationToken cancellationToken = default) =>
        Update(eventId, e =>
        {
            e.Status = "Failed";
            e.Attempts++;
            e.NextRetryUtc = null;
            e.LastError = reason;
        });

    public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
        Count("Pending");

    public Task<int> GetFailedCountAsync(CancellationToken cancellationToken = default) =>
        Count("Failed");

    private Task Update(Guid eventId, Action<OutboxEvent> change)
    {
        lock (_gate)
            change(_events.Single(e => e.Id == eventId));

        return Task.CompletedTask;
    }

    private Task<int> Count(string status)
    {
        lock (_gate)
            return Task.FromResult(_events.Count(e => e.Status == status));
    }
}
