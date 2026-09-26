using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>Append-only by design: POST and GET only. There is deliberately no PUT or DELETE.</summary>
internal static class CashCountEndpoints
{
    public static void MapCashCounts(this RouteGroupBuilder cash)
    {
        var counts = cash.MapGroup("/counts");

        counts.MapGet("", async (
            IQueryHandler<GetCashCountsQuery, IReadOnlyList<CashCountDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashCountsQuery(businessDate), cancellationToken)));

        counts.MapPost("", async (
            ICommandHandler<AddCashCountCommand, CashCountDto> handler,
            ClaimsPrincipal user,
            AddCashCountRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new AddCashCountCommand(user.GetRequiredUserId(), request), cancellationToken);
            return Results.Created($"/cash/counts/{result.Id}", result);
        });
    }
}
