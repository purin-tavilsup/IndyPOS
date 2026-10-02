using System.Security.Claims;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.StoreHub.Endpoints.Cash;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

internal static class SaleQueryEndpoints
{
    public static void MapSaleQueries(this RouteGroupBuilder sales)
    {
        sales.MapGet("", async (
            IQueryHandler<ListSalesQuery, SalesPage> handler,
            ClaimsPrincipal user,
            string? from,
            string? to,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            // Dates bind as strings so a malformed one is our Thai 400, not the framework's bare one.
            var query = new ListSalesQuery(
                From: SalesQueryRules.ParseDate(from),
                To: SalesQueryRules.ParseDate(to),
                Page: page ?? SalesQueryRules.FirstPage,
                PageSize: pageSize ?? SalesQueryRules.DefaultPageSize,
                CanViewAnyDay: CanViewAnyDay(user));

            return Results.Ok(await handler.HandleAsync(query, cancellationToken));
        });

        sales.MapGet("/{id:guid}", async (
            IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetSaleByIdQuery(id, CanViewAnyDay(user)), cancellationToken)
                       ?? throw new SaleNotFoundException()));

        sales.MapGet("/{number:long}", async (
            IQueryHandler<GetSaleByNumberQuery, InvoiceDetailDto?> handler,
            ClaimsPrincipal user,
            long number,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetSaleByNumberQuery(number, CanViewAnyDay(user)), cancellationToken)
                       ?? throw new SaleNotFoundException(number)));

        // Typed constraints alone answer "/sales/abc" or an overflowing number with 404 (no route
        // matched). An unconstrained parameter has LOWER routing precedence than the two above, so
        // it only catches what they reject, and turns it into the 400 the spec asks for.
        sales.MapGet("/{value}", (string value) =>
            Results.BadRequest(new { error = SalesQueryRules.InvalidNumberMessage(value) }));
    }

    private static bool CanViewAnyDay(ClaimsPrincipal user) => user.HasCapability(Capability.ReportsView);
}
