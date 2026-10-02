using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Validation;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetProductSales;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetSalesSummary;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Reports;

/// <summary>
/// Aggregates for the back office, all behind reports.view. /reports/legacy/* answer in the WinForms
/// models' shape (for WinForms compatibility).
/// </summary>
public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        MapSalesSummary(app);
        MapPayLaterReport(app);
        MapProductSales(app);
        MapLegacySalesSummary(app);
        MapLegacyPaymentsSummary(app);
        return app;
    }

    // Sales summary (daily/weekly/monthly dashboard)
    private static void MapSalesSummary(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/sales-summary", async (
            IQueryHandler<GetSalesSummaryQuery, SalesSummaryDto> handler,
            DateOnly fromDate,
            DateOnly toDate,
            int? topProductsCount,
            CancellationToken cancellationToken) =>
        {
            if (RejectInvalidRange(fromDate, toDate) is { } rejection)
            {
                return rejection;
            }

            var query = new GetSalesSummaryQuery(
                FromDate: fromDate,
                ToDate: toDate,
                TopProductsCount: topProductsCount ?? 10);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // PayLater (accounts receivable) report
    private static void MapPayLaterReport(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/pay-later", async (
            IQueryHandler<GetPayLaterReportQuery, PayLaterReportDto> handler,
            bool? includeCompleted,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPayLaterReportQuery(
                IncludeCompleted: includeCompleted ?? false,
                Page: page ?? 1,
                PageSize: pageSize ?? 50);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // Product sales report
    private static void MapProductSales(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/product-sales", async (
            IQueryHandler<GetProductSalesQuery, PagedResult<ProductSalesDto>> handler,
            DateOnly fromDate,
            DateOnly toDate,
            string? category,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            if (RejectInvalidRange(fromDate, toDate) is { } rejection)
            {
                return rejection;
            }

            var query = new GetProductSalesQuery(
                FromDate: fromDate,
                ToDate: toDate,
                Category: category,
                Page: page ?? 1,
                PageSize: pageSize ?? 50);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // Legacy sales summary (returns SalesSummary model)
    private static void MapLegacySalesSummary(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/legacy/sales-summary", async (
            IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> handler,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken) =>
        {
            if (RejectInvalidRange(fromDate, toDate) is { } rejection)
            {
                return rejection;
            }

            var query = new GetLegacySalesSummaryQuery(fromDate, toDate);
            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // Legacy payments summary (returns PaymentsSummary model)
    private static void MapLegacyPaymentsSummary(IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/legacy/payments-summary", async (
            IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary> handler,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken) =>
        {
            if (RejectInvalidRange(fromDate, toDate) is { } rejection)
            {
                return rejection;
            }

            var query = new GetLegacyPaymentsSummaryQuery(fromDate, toDate);
            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization("CanViewReports");
    }

    // Checked before the handler runs: ReportDateRange.ToUtcRange throws on a swapped range and
    // overflows on 9999-12-31, and either escaped as a 500.
    private static IResult? RejectInvalidRange(DateOnly fromDate, DateOnly toDate) =>
        DateRangeRule.FindViolation(fromDate, toDate) is { } error
            ? Results.BadRequest(new { error })
            : null;
}
