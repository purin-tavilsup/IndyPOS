using System.Security.Claims;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.CloudApi.Infrastructure;

namespace IndyPOS.CloudApi.Endpoints;

/// <summary>
/// A store's heartbeat: StoreHub reports every two minutes that it is up and how its outbox stands, and the reply
/// carries the resend requests an admin made for it. It takes the same store token as /sync/events.
/// </summary>
internal static class SyncHeartbeatEndpoints
{
    public static IEndpointRouteBuilder MapSyncHeartbeats(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sync/heartbeats", async (
            CloudDbContext db,
            HeartbeatRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            // Every store token carries store_id; one without it is not a store's token.
            var storeId = user.FindFirst("store_id")?.Value;
            if (string.IsNullOrEmpty(storeId))
                return Results.Forbid();

            return Results.Ok(await HeartbeatRecorder.RecordAsync(db, storeId, request, DateTime.UtcNow, cancellationToken));
        }).RequireAuthorization();

        return app;
    }
}
