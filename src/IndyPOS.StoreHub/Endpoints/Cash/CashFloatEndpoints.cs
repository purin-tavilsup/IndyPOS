using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.StoreHub.Endpoints.Common;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class CashFloatEndpoints
{
    public static void MapCashFloats(this RouteGroupBuilder cash)
    {
        var floats = cash.MapGroup("/floats");

        floats.MapGet("", async (
            IQueryHandler<GetCashFloatsQuery, IReadOnlyList<CashFloatDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashFloatsQuery(businessDate), cancellationToken)));

        floats.MapPost("", async (
            ICommandHandler<AddCashFloatCommand, CashFloatDto> handler,
            ClaimsPrincipal user,
            AddCashFloatRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new AddCashFloatCommand(user.GetRequiredUserId(), request.Amount, request.Description);
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/cash/floats/{result.Id}", result);
        });

        floats.MapPut("/{id:guid}", async (
            ICommandHandler<EditCashFloatCommand, CashFloatDto> handler,
            ClaimsPrincipal user,
            Guid id,
            EditCashFloatRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new EditCashFloatCommand(id, user.GetRequiredUserId(), request.Amount, request.Description);
            return Results.Ok(await handler.HandleAsync(command, cancellationToken));
        });

        floats.MapDelete("/{id:guid}", async (
            ICommandHandler<DeleteCashEntryCommand<CashFloat>> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCashEntryCommand<CashFloat>(id, user.GetRequiredUserId()), cancellationToken);
            return Results.NoContent();
        });
    }
}
