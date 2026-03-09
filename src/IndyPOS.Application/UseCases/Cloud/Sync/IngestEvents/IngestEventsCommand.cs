using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;

/// <summary>
/// Command to ingest sync events from stores.
/// </summary>
public record IngestEventsCommand(IReadOnlyList<SyncEventRequest> Events) : ICommand<SyncEventsResponse>;
