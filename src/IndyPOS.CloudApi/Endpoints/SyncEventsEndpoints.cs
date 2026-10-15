using System.Security.Claims;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.Cloud.Sync;
using IndyPOS.Application.UseCases.Cloud.Sync.IngestEvents;
using Nokpirab;

namespace IndyPOS.CloudApi.Endpoints;

/// <summary>
/// Idempotent event ingestion from the stores (F2). It takes a store token, and every event must be
/// for that token's store.
/// </summary>
internal static class SyncEventsEndpoints
{
    public static IEndpointRouteBuilder MapSyncEvents(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sync/events", async (
            ICommandHandler<IngestEventsCommand, SyncEventsResponse> handler,
            SyncEventsRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var command = new IngestEventsCommand(request.Events, user.FindFirst("store_id")?.Value ?? string.Empty);

            // 403, not a 200 with Accepted=false: HttpCloudSyncClient treats any 2xx as sent, so only a
            // failure status makes the store keep the event and back off for manual review.
            try
            {
                return Results.Ok(await handler.HandleAsync(command, cancellationToken));
            }
            catch (StoreMismatchException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
        }).RequireAuthorization();

        return app;
    }
}
