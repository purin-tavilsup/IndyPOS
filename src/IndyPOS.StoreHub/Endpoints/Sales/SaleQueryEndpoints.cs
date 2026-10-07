using System.Security.Claims;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.StoreHub.Endpoints.Common;
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

        sales.MapGet("/lines", async (
            IQueryHandler<ListSaleLinesQuery, SaleLinesPage> handler,
            ClaimsPrincipal user,
            string? from,
            string? to,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var query = new ListSaleLinesQuery(
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

        // maxlength(19) keeps this route disjoint from {id:guid}: a 32-digit segment is a valid
        // "N"-format GUID that also parses as a long, and two equal matches would be a 500. No bill
        // number needs more digits than long.MaxValue's 19.
        sales.MapGet("/{number:long:maxlength(19)}", async (
            IQueryHandler<GetSaleByNumberQuery, InvoiceDetailDto?> handler,
            ClaimsPrincipal user,
            long number,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetSaleByNumberQuery(number, CanViewAnyDay(user)), cancellationToken)
                       ?? throw new SaleNotFoundException(number)));

        // Typed constraints alone answer "/sales/abc", an overflowing number or a number longer than
        // 19 characters (unless it is also a GUID) with 404, because no route matched. An
        // unconstrained parameter has LOWER routing precedence than the two above, so it
        // only catches what they reject, and turns it into the 400 the spec asks for. Zero and negative
        // numbers are valid longs: they match {number:long} and are rejected in the handler by
        // SalesQueryRules.EnsureValidNumber, with the same Thai message.
        sales.MapGet("/{value}", (string value) =>
            Results.BadRequest(new { error = SalesQueryRules.InvalidNumberMessage(value) }));
    }

    private static bool CanViewAnyDay(ClaimsPrincipal user) => user.HasCapability(Capability.ReportsView);
}
