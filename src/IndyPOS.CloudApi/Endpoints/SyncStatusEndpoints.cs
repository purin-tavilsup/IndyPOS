using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Endpoints;

internal static class SyncStatusEndpoints
{
    public static IEndpointRouteBuilder MapSyncStatus(this IEndpointRouteBuilder app)
    {
        // Sync status endpoint
        app.MapGet("/sync/status", async (CloudDbContext db, CancellationToken cancellationToken) =>
        {
            var totalEvents = await db.SyncedEvents.CountAsync(cancellationToken);
            var unprocessedEvents = await db.SyncedEvents.CountAsync(e => e.ProcessedAtUtc == null, cancellationToken);
            var processedEvents = await db.ProcessedEvents.CountAsync(cancellationToken);
            var totalInvoices = await db.Invoices.CountAsync(cancellationToken);

            return Results.Ok(new
            {
                status = "running",
                storage = "postgresql",
                totalEvents,
                unprocessedEvents,
                processedEvents,
                totalInvoices,
                timestamp = DateTime.UtcNow
            });
        });

        return app;
    }
}
