using System.Text.Json;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;

/// <summary>
/// Handler for ingesting sync events from stores.
/// Implements idempotent event ingestion - duplicate events are accepted but not stored twice.
/// </summary>
public class IngestEventsCommandHandler(
    ISyncedEventRepository repository,
    ILogger<IngestEventsCommandHandler> logger)
    : ICommandHandler<IngestEventsCommand, SyncEventsResponse>
{
    public async Task<SyncEventsResponse> HandleAsync(
        IngestEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Ingesting {EventCount} events", command.Events.Count);

        EnsureAllForAuthenticatedStore(command);

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
                    logger.LogDebug(
                        "Duplicate event skipped: EventId={EventId}, Type={EventType}, StoreId={StoreId}",
                        eventRequest.EventId, eventRequest.EventType, eventRequest.StoreId);
                    results.Add(new SyncEventResult(eventRequest.EventId, Accepted: true, Reason: "duplicate"));
                    continue;
                }

                // Store the event
                // SourceStoreId is safe to take from the token: EnsureAllForAuthenticatedStore above has
                // already refused the batch unless every payload names this store.
                var entity = new SyncedEventEntity
                {
                    EventId = eventRequest.EventId,
                    StoreId = eventRequest.StoreId,
                    SourceStoreId = command.AuthenticatedStoreId,
                    EventType = eventRequest.EventType,
                    Payload = eventRequest.Payload,
                    CreatedAtUtc = eventRequest.CreatedAtUtc,
                    ReceivedAtUtc = receivedAt
                };

                await repository.AddAsync(entity, cancellationToken);
                acceptedCount++;
                logger.LogDebug(
                    "Event ingested: EventId={EventId}, Type={EventType}, StoreId={StoreId}",
                    eventRequest.EventId, eventRequest.EventType, eventRequest.StoreId);
                results.Add(new SyncEventResult(eventRequest.EventId, Accepted: true));
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogError(
                    ex, "Failed to ingest event: EventId={EventId}, Type={EventType}, StoreId={StoreId}",
                    eventRequest.EventId, eventRequest.EventType, eventRequest.StoreId);
                results.Add(new SyncEventResult(eventRequest.EventId, Accepted: false, Reason: ex.Message));
            }
        }

        logger.LogInformation(
            "Event ingestion complete: Accepted={Accepted}, Duplicates={Duplicates}, Failed={Failed}",
            acceptedCount, duplicateCount, failedCount);

        return new SyncEventsResponse(acceptedCount, duplicateCount, failedCount, results);
    }

    /// <remarks>
    /// The payload's StoreId is what EventProcessor materialises under, so that is what must match
    /// the token. The envelope's int StoreId cannot be used: real store ids are strings such as
    /// "STORE-001" or a UUID, and HttpCloudSyncClient.ParseStoreId sends 0 for all of them.
    /// Checked for the whole batch before anything is stored, so a rejected batch leaves no trace.
    /// </remarks>
    private static void EnsureAllForAuthenticatedStore(IngestEventsCommand command)
    {
        var foreign = command.Events.FirstOrDefault(e => PayloadStoreId(e.Payload) != command.AuthenticatedStoreId);

        if (foreign is not null)
            throw new StoreMismatchException(
                $"Event {foreign.EventId} is not for store '{command.AuthenticatedStoreId}', the store this token authenticates.");
    }

    /// <summary>The payload's top-level string StoreId, or null when there is none to read.</summary>
    /// <remarks>
    /// The payload is null-checked, although its type says it cannot be null: JSON binding puts a
    /// runtime null there for "payload": null, and JsonDocument.Parse would throw on it -- a 500
    /// instead of the 403 for an unreadable payload.
    /// </remarks>
    private static string? PayloadStoreId(string? payload)
    {
        if (payload is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(payload);

            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty("StoreId", out var storeId)
                   && storeId.ValueKind == JsonValueKind.String
                ? storeId.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
