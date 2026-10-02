using IndyPOS.StoreHub.Endpoints.Cash;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>
/// Bills as one REST resource (spec §6). sales.reprint opens it; reports.view lifts the today-only
/// limit. POST /sales/complete is still mapped in Program.cs — renaming it is the route tidy-up PR.
/// </summary>
public static class SalesEndpoints
{
    public const string Policy = "CanReprintSales";

    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var sales = app.MapGroup("/sales")
                       .RequireAuthorization(Policy)
                       .AddEndpointFilter<RequireUserIdFilter>()
                       .AddEndpointFilter<SalesExceptionFilter>();

        sales.MapSaleQueries();
        return app;
    }
}
