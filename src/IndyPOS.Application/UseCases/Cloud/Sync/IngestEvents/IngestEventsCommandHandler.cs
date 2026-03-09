using IndyPOS.Application.Abstractions.Cloud.Repositories;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;

/// <summary>
/// Handler for ingesting sync events from stores.
/// Implements idempotent event ingestion - duplicate events are accepted but not stored twice.
/// </summary>
public class IngestEventsCommandHandler(ISyncedEventRepository repository)
    : ICommandHandler<IngestEventsCommand, SyncEventsResponse>
{
    public async Task<SyncEventsResponse> HandleAsync(
        IngestEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SyncEventResult>();
        var acceptedCount = 0;
        var duplicateCount = 0;
        var failedCount = 0;
        var receivedAt = DateTime.UtcNow;

        foreach (var eventRequest in command.Events)
        {
            try
            {
                // Idempotency check - skip if already exists
                var exists = await repository.ExistsAsync(eventRequest.EventId, cancellationToken);
                if (exists)
                {
                    duplicateCount++;
                    results.Add(new SyncEventResult(eventRequest.EventId, Accepted: true, Reason: "duplicate"));
                    continue;
                }

                // Store the event
                var entity = new SyncedEventEntity
                {
                    EventId = eventRequest.EventId,
                    StoreId = eventRequest.StoreId,
                    EventType = eventRequest.EventType,
                    Payload = eventRequest.Payload,
                    CreatedAtUtc = eventRequest.CreatedAtUtc,
                    ReceivedAtUtc = receivedAt
                };

                await repository.AddAsync(entity, cancellationToken);
                acceptedCount++;
                results.Add(new SyncEventResult(eventRequest.EventId, Accepted: true));
            }
            catch (Exception ex)
            {
                failedCount++;
                results.Add(new SyncEventResult(eventRequest.EventId, Accepted: false, Reason: ex.Message));
            }
        }

        return new SyncEventsResponse(acceptedCount, duplicateCount, failedCount, results);
    }
}
