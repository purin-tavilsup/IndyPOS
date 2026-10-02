using System.Security.Claims;
using IndyPOS.CloudApi.Infrastructure;

namespace IndyPOS.CloudApi.Endpoints;

/// <summary>
/// The cloud's sync health for one store. It takes the same store token as /sync/events and counts
/// only that token's store. It used to be open and to count every store, which told any caller how
/// much every other store trades.
/// </summary>
internal static class SyncStatusEndpoints
{
    public static IEndpointRouteBuilder MapSyncStatus(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sync/status", async (CloudDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            // Every store token carries store_id (TokenController); one without it is not a store's token.
            var storeId = user.FindFirst("store_id")?.Value;
            if (string.IsNullOrEmpty(storeId))
                return Results.Forbid();

            var counts = await StoreSyncStatusQuery.CountAsync(db, storeId, cancellationToken);

            return Results.Ok(new
            {
                status = "running",
                storage = "postgresql",
                totalEvents = counts.TotalEvents,
                unprocessedEvents = counts.UnprocessedEvents,
                processedEvents = counts.ProcessedEvents,
                totalInvoices = counts.TotalInvoices,
                timestamp = DateTime.UtcNow
            });
        }).RequireAuthorization();

        return app;
    }
}
