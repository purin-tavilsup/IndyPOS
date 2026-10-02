using System.Security.Claims;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.StoreHub.Endpoints.Cash;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>A reprint is a created record (201), not an action verb — spec §6.</summary>
internal static class SaleReprintEndpoints
{
    public static void MapSaleReprints(this RouteGroupBuilder sales) =>
        sales.MapPost("/{id:guid}/reprints", async (
            ICommandHandler<CreateInvoiceReprintCommand, InvoiceReprintResultDto> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateInvoiceReprintCommand(id, user.GetRequiredUserId(), user.HasCapability(Capability.ReportsView));
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/sales/{id}/reprints/{result.Reprint.Id}", result);
        });
}
