using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.StoreHub.Endpoints.Common;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class DebtRepaymentEndpoints
{
    public static void MapDebtRepayments(this RouteGroupBuilder cash)
    {
        var repayments = cash.MapGroup("/debt-repayments");

        repayments.MapGet("", async (
            IQueryHandler<GetDebtRepaymentsQuery, IReadOnlyList<DebtRepaymentDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetDebtRepaymentsQuery(businessDate), cancellationToken)));

        repayments.MapPost("", async (
            ICommandHandler<AddDebtRepaymentCommand, DebtRepaymentDto> handler,
            ClaimsPrincipal user,
            AddDebtRepaymentRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new AddDebtRepaymentCommand(user.GetRequiredUserId(), request.CustomerName, request.Amount);
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/cash/debt-repayments/{result.Id}", result);
        });

        repayments.MapPut("/{id:guid}", async (
            ICommandHandler<EditDebtRepaymentCommand, DebtRepaymentDto> handler,
            ClaimsPrincipal user,
            Guid id,
            EditDebtRepaymentRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new EditDebtRepaymentCommand(id, user.GetRequiredUserId(), request.CustomerName, request.Amount);
            return Results.Ok(await handler.HandleAsync(command, cancellationToken));
        });

        repayments.MapDelete("/{id:guid}", async (
            ICommandHandler<DeleteCashEntryCommand<DebtRepayment>> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCashEntryCommand<DebtRepayment>(id, user.GetRequiredUserId()), cancellationToken);
            return Results.NoContent();
        });
    }
}
