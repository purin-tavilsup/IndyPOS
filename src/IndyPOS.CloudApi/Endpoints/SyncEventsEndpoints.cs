using System.Security.Claims;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;
using IndyPOS.CloudApi.Infrastructure;
using Nokpirab;

namespace IndyPOS.CloudApi.Endpoints;

/// <summary>
/// Idempotent event ingestion from the stores (F2). It takes a store token, and every event must be
/// for that token's store. Every refusal is also logged, and a later accept resolves it (SyncRejectionLog).
/// </summary>
internal static class SyncEventsEndpoints
{
    public static IEndpointRouteBuilder MapSyncEvents(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sync/events", async (
            ICommandHandler<IngestEventsCommand, SyncEventsResponse> handler,
            CloudDbContext db,
            ILoggerFactory loggerFactory,
            SyncEventsRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var storeId = user.FindFirst("store_id")?.Value ?? string.Empty;
            var command = new IngestEventsCommand(request.Events, storeId);
            var log = new RejectionLogging(db, loggerFactory.CreateLogger(nameof(SyncEventsEndpoints)), storeId, request.Events);

            // A marked 400 or 403, not a 200 with Accepted=false: StoreHub treats only a refusal carrying
            // SyncRefusal's marker as final and parks the event as Failed, kept for a resend. Every other
            // failure status, and Accepted=false, is retried.
            try
            {
                var response = await handler.HandleAsync(command, cancellationToken);
                await log.ResolveAsync(response, cancellationToken);
                return Results.Ok(response);
            }
            catch (MalformedSyncBatchException ex)
            {
                await log.RecordAsync(StatusCodes.Status400BadRequest, $"{SyncRefusal.MalformedBatch}: {ex.Message}", cancellationToken);
                return Refusal(ex.Message, StatusCodes.Status400BadRequest, SyncRefusal.MalformedBatch);
            }
            catch (StoreMismatchException ex)
            {
                await log.RecordAsync(StatusCodes.Status403Forbidden, $"{SyncRefusal.StoreMismatch}: {ex.Message}", cancellationToken);
                return Refusal(ex.Message, StatusCodes.Status403Forbidden, SyncRefusal.StoreMismatch);
            }
        }).RequireAuthorization();

        return app;
    }

    private static IResult Refusal(string detail, int statusCode, string refusal) =>
        Results.Problem(
            detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { [SyncRefusal.ExtensionKey] = refusal });

    /// <summary>
    /// Writes to the rejection log without ever changing what the store is told: the answer to the store decides
    /// whether its events are kept or parked, and a log that cannot be written must not turn that into a 500.
    /// </summary>
    private sealed class RejectionLogging(CloudDbContext db, ILogger logger, string storeId, IReadOnlyList<SyncEventRequest?>? events)
    {
        public Task RecordAsync(int statusCode, string reason, CancellationToken ct) =>
            SafelyAsync(() => SyncRejectionLog.RecordAsync(db, storeId, events, statusCode, reason, DateTime.UtcNow, ct));

        public Task ResolveAsync(SyncEventsResponse response, CancellationToken ct)
        {
            var accepted = response.Results.Where(r => r.Accepted).Select(r => r.EventId).ToList();
            return SafelyAsync(() => SyncRejectionLog.ResolveAsync(db, storeId, accepted, DateTime.UtcNow, ct));
        }

        private async Task SafelyAsync(Func<Task> write)
        {
            try
            {
                await write();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Could not update the sync rejection log for store {StoreId}", storeId);
                db.ChangeTracker.Clear();
            }
        }
    }
}
