using System.Security.Claims;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.StoreHub.Endpoints.Common;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

internal static class SaleCompletionEndpoints
{
    public static void MapSaleCompletion(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sales", async (
            ICommandHandler<CompleteSaleCommand, CompleteSaleResponse> handler,
            IStoreIdentityService storeIdentity,
            ClaimsPrincipal user,
            CompleteSaleRequest request,
            CancellationToken cancellationToken) =>
        {
            // The seller is whoever the token says. The body has no user field: one once let any caller
            // ring a sale up as someone else.
            var command = new CompleteSaleCommand(
                StoreId: storeIdentity.StoreId,
                UserId: user.GetRequiredUserId(),
                Lines: request.Lines,
                Payments: request.Payments);

            // A refused sale is the caller's mistake, not the server's: a Thai reason for the cashier.
            try
            {
                var sale = await handler.HandleAsync(command, cancellationToken);
                return Results.Created($"/sales/{sale.InvoiceId}", sale);
            }
            catch (SaleValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization("CanCompleteSales")
          .AddEndpointFilter<RequireUserIdFilter>();
    }
}
