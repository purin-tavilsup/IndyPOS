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

            // A marked 400 or 403, not a 200 with Accepted=false: StoreHub treats only a refusal carrying
            // SyncRefusal's marker as final and parks the event as Failed, kept for a resend. Every other
            // failure status, and Accepted=false, is retried.
            try
            {
                return Results.Ok(await handler.HandleAsync(command, cancellationToken));
            }
            catch (MalformedSyncBatchException ex)
            {
                return Refusal(ex.Message, StatusCodes.Status400BadRequest, SyncRefusal.MalformedBatch);
            }
            catch (StoreMismatchException ex)
            {
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
}
