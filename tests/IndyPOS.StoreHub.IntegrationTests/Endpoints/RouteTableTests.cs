using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Pins the verb, path and policy of every route that Program.cs mapped inline before the routes moved
/// into Endpoints/. Most of these routes have no HTTP test of their own, so this test is what catches
/// a move that drops a RequireAuthorization or changes a verb. Endpoint filters are not endpoint
/// metadata, so RequireUserIdFilter is proved by behaviour tests instead
/// (CompleteSale_WithATokenWithoutAUserId_ReturnsUnauthorized). It also pins that each hard-renamed route
/// is gone.
/// </summary>
[Collection("Integration")]
public class RouteTableTests : IntegrationTestBase
{
    private const string Anonymous = "(anonymous)";
    private const string AnyAuthenticatedUser = "(authenticated)";

    private static readonly string[] MovedRoutes =
    [
        $"GET / {Anonymous}",
        $"GET /version {Anonymous}",
        $"POST /auth/login {Anonymous}",
        $"GET /auth/me {AnyAuthenticatedUser}",
        $"POST /auth/change-password {AnyAuthenticatedUser}",
        "GET /products CanReadProducts",
        "GET /products/stock CanReadProducts",
        "POST /products CanManageProducts",
        "PUT /products/{id:guid} CanManageProducts",
        "DELETE /products/{id:guid} CanManageProducts",
        "POST /products/{id:guid}/stock-adjustments CanAdjustInventory",
        "POST /products/next-barcode CanManageProducts",
        "GET /payment-methods CanReadProducts",
        "POST /payment-methods CanManagePaymentMethods",
        "PATCH /payment-methods/{code} CanManagePaymentMethods",
        "GET /product-categories CanReadProducts",
        $"GET /store/features {AnyAuthenticatedUser}",
        "POST /sales CanCompleteSales",
        "GET /sales/lines CanReprintSales",
        "GET /sync/status CanViewSyncStatus",
        "GET /reports/sales-summary CanViewReports",
        "GET /reports/pay-later CanViewReports",
        "GET /reports/product-sales CanViewReports",
        "GET /reports/legacy/sales-summary CanViewReports",
        "GET /reports/legacy/payments-summary CanViewReports",
        $"GET /pay-later {AnyAuthenticatedUser}",
        $"GET /pay-later/{{id:guid}} {AnyAuthenticatedUser}",
        $"POST /pay-later/{{id:guid}}/payments {AnyAuthenticatedUser}"
    ];

    // Hard renames (no alias before go-live). A task that renames a route adds its old pattern here.
    private static readonly string[] RemovedRoutes =
    [
        "GET /admin/payment-methods CanManagePaymentMethods",
        "POST /admin/payment-methods CanManagePaymentMethods",
        "PATCH /admin/payment-methods/{code} CanManagePaymentMethods",
        "POST /sales/complete CanCompleteSales",
        $"POST /pay-later/{{id:guid}}/record-payment {AnyAuthenticatedUser}",
        "POST /products/{id:guid}/adjust-quantity CanAdjustInventory"
    ];

    public RouteTableTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public void RouteTable_WithTheAppBuilt_ContainsEveryMovedRouteWithItsPolicy()
    {
        var table = DescribeRouteTable();

        table.Should()
             .Contain(MovedRoutes)
             .And
             .OnlyHaveUniqueItems("a route left in Program.cs and also mapped from its new file would appear twice");
    }

    [Fact]
    public void RouteTable_WithTheAppBuilt_ContainsNoRemovedRoute()
    {
        var table = DescribeRouteTable();

        table.Should()
             .NotContain(RemovedRoutes);
    }

    private List<string> DescribeRouteTable() =>
        Factory.Services.GetRequiredService<EndpointDataSource>()
                        .Endpoints
                        .OfType<RouteEndpoint>()
                        .Select(Describe)
                        .ToList();

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];
        var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                                        .Select(a => a.Policy ?? AnyAuthenticatedUser)
                                        .ToList();
        var policy = policies.Count == 0 ? Anonymous : string.Join(",", policies);

        return $"{string.Join(",", methods)} {endpoint.RoutePattern.RawText} {policy}";
    }
}
