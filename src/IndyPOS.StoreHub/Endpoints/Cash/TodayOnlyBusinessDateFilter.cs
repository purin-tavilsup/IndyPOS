using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.StoreHub.Endpoints.Common;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>
/// Spec §5: today's cash figures are the cashier's to count; another day's are a report. Applied to
/// the whole /cash group rather than per route, so a new read that takes a businessDate cannot
/// forget the rule — five routes shipped in #97 without it.
/// </summary>
/// <remarks>
/// The only <see cref="DateOnly"/> any /cash route binds is its <c>businessDate</c> query value; the
/// writes take request records. A null (omitted) date means today and passes. A malformed date never
/// gets here: binding answers it with 400 before any filter runs.
/// </remarks>
internal sealed class TodayOnlyBusinessDateFilter(ICashDrawerClock clock) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var day in context.Arguments.OfType<DateOnly>())
        {
            TodayOnlyRule.EnsureAllowed(
                day,
                clock.Now().BusinessDate,
                context.HttpContext.User.HasCapability(Capability.ReportsView));
        }

        return next(context);
    }
}
