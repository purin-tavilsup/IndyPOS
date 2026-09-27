using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Payouts;

public class GetCashPayoutsQueryHandlerTests
{
    private static GetCashPayoutsQueryHandler HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock, context.StoreIdentity);

    [Fact]
    public async Task HandleAsync_WithSoftDeletedPayout_ExcludesIt()
    {
        await using var context = new CashDrawerTestContext();
        var deleted = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        var result = await HandlerFor(context).HandleAsync(new GetCashPayoutsQuery(null));

        result.Should()
              .NotContain(p => p.Id == deleted.Id);
    }

    [Fact]
    public async Task HandleAsync_WithoutDate_ReturnsOnlyToday()
    {
        await using var context = new CashDrawerTestContext();
        var yesterdays = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);
        var todays = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(new GetCashPayoutsQuery(null));

        result.Select(p => p.Id).Should()
                                .Equal(todays.Id);
    }

    [Fact]
    public async Task HandleAsync_WithPastDate_ReturnsThatDay()
    {
        await using var context = new CashDrawerTestContext();
        var yesterdays = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        var result = await HandlerFor(context).HandleAsync(new GetCashPayoutsQuery(CashDrawerTestContext.Yesterday));

        result.Select(p => p.Id).Should()
                                .Equal(yesterdays.Id);
    }
}
