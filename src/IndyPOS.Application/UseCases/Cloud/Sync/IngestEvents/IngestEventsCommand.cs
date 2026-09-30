using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;

/// <summary>
/// Command to ingest sync events from stores.
/// </summary>
/// <param name="AuthenticatedStoreId">The store_id claim of the caller's token: the only store it may write for.</param>
public record IngestEventsCommand(IReadOnlyList<SyncEventRequest> Events, string AuthenticatedStoreId) : ICommand<SyncEventsResponse>;
