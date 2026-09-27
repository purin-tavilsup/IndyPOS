using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class CashPayoutEndpoints
{
    public static void MapCashPayouts(this RouteGroupBuilder cash)
    {
        var payouts = cash.MapGroup("/payouts");

        payouts.MapGet("", async (
            IQueryHandler<GetCashPayoutsQuery, IReadOnlyList<CashPayoutDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashPayoutsQuery(businessDate), cancellationToken)));

        payouts.MapPost("", async (
            ICommandHandler<AddCashPayoutCommand, CashPayoutDto> handler,
            ClaimsPrincipal user,
            AddCashPayoutRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new AddCashPayoutCommand(user.GetRequiredUserId(), request.Amount, request.Category, request.Description);
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/cash/payouts/{result.Id}", result);
        });

        payouts.MapPut("/{id:guid}", async (
            ICommandHandler<EditCashPayoutCommand, CashPayoutDto> handler,
            ClaimsPrincipal user,
            Guid id,
            EditCashPayoutRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new EditCashPayoutCommand(id, user.GetRequiredUserId(), request.Amount, request.Category, request.Description);
            return Results.Ok(await handler.HandleAsync(command, cancellationToken));
        });

        payouts.MapDelete("/{id:guid}", async (
            ICommandHandler<DeleteCashEntryCommand<CashPayout>> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCashEntryCommand<CashPayout>(id, user.GetRequiredUserId()), cancellationToken);
            return Results.NoContent();
        });
    }
}
