using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.StoreHub.Endpoints.Common;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>
/// The ลิ้นชักเก็บเงิน API: every /cash route, behind one policy and the group's three filters.
/// </summary>
public static class CashEndpoints
{
    public const string Policy = "CanManageCash";

    public static IEndpointRouteBuilder MapCashEndpoints(this IEndpointRouteBuilder app)
    {
        var cash = app.MapGroup("/cash")
                      .RequireAuthorization(Policy)
                      .AddEndpointFilter<RequireUserIdFilter>()
                      .AddEndpointFilter<CashExceptionFilter>()
                      .AddEndpointFilter<TodayOnlyBusinessDateFilter>();

        cash.MapGet("/summary", async (
            IQueryHandler<GetCashDrawerSummaryQuery, CashDrawerSummaryDto> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashDrawerSummaryQuery(businessDate), cancellationToken)));

        cash.MapCashPayouts();
        cash.MapCashFloats();
        cash.MapDebtRepayments();
        cash.MapCashCounts();
        return app;
    }
}
