using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.Extensions.Options;

namespace IndyPOS.CloudApi.Endpoints;

/// <summary>
/// The admin page's view of store sync health, and its Resend button. Behind SystemAdminOnly for now; cloud slice 1
/// replaces that policy with the Admin person token from IndyPOS.Cloud.Auth.
/// </summary>
internal static class AdminSyncHealthEndpoints
{
    public sealed record ResendRequestBody(Guid? EventId);

    public static IEndpointRouteBuilder MapAdminSyncHealth(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/stores/sync-health", async (
            CloudDbContext db,
            IOptions<StoreSyncHealthOptions> options,
            CancellationToken cancellationToken) =>
            Results.Ok(await StoreSyncHealthQuery.ListAsync(db, options.Value, DateTime.UtcNow, cancellationToken)))
           .RequireAuthorization("SystemAdminOnly");

        app.MapPost("/admin/stores/{storeId}/resend-requests", async (
            string storeId,
            ResendRequestBody? body,
            CloudDbContext db,
            CancellationToken cancellationToken) =>
        {
            var result = await ResendRequests.RequestAsync(db, storeId, body?.EventId, DateTime.UtcNow, cancellationToken);

            return result switch
            {
                null => Results.NotFound(new { error = $"Store '{storeId}' is not registered." }),
                { Created: true } created => Results.Created($"/admin/stores/{storeId}/resend-requests/{created.RequestId}", new { requestId = created.RequestId }),
                { } open => Results.Ok(new { requestId = open.RequestId })
            };
        }).RequireAuthorization("SystemAdminOnly");

        return app;
    }
}
