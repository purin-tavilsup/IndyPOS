using IndyPOS.Application.Abstractions.StoreHub.Repositories;

namespace IndyPOS.StoreHub.Endpoints.Sync;

/// <summary>The outbox's health: how many events still wait to reach the cloud, and how many failed.</summary>
public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        // Sync status endpoint
        app.MapGet("/sync/status", async (
            IOutboxRepository outboxRepository,
            CancellationToken cancellationToken) =>
        {
            var pendingCount = await outboxRepository.GetPendingCountAsync(cancellationToken);
            var failedCount = await outboxRepository.GetFailedCountAsync(cancellationToken);

            return Results.Ok(new
            {
                status = pendingCount == 0 ? "synced" : "pending",
                pending = pendingCount,
                failed = failedCount,
                timestamp = DateTime.UtcNow
            });
        }).RequireAuthorization("CanViewSyncStatus");

        return app;
    }
}
