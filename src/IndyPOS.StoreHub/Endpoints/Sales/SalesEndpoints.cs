using IndyPOS.StoreHub.Endpoints.Common;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>
/// Bills as one REST resource (invoice-history v4 spec §6). sales.reprint opens the /sales group;
/// reports.view lifts the today-only limit. POST /sales is mapped beside the group, not inside it: it needs
/// CanCompleteSales instead of the group's policy, and it answers its own 400.
/// </summary>
public static class SalesEndpoints
{
    public const string Policy = "CanReprintSales";

    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSaleCompletion();

        var sales = app.MapGroup("/sales")
                       .RequireAuthorization(Policy)
                       .AddEndpointFilter<RequireUserIdFilter>()
                       .AddEndpointFilter<SalesExceptionFilter>();

        sales.MapSaleQueries();
        sales.MapSaleReprints();
        return app;
    }
}
