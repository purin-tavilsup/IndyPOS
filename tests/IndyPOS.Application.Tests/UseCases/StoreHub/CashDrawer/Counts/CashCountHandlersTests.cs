using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Counts;

public class CashCountHandlersTests
{
    private static readonly AddCashCountRequest MorningCount = new(1, 0, 5, 0, 0, 10, 0, 0, 3);   // 1×1000 + 5×100 + 10×10 + 3×1 = 1603
    private static readonly AddCashCountRequest EveningCount = new(2, 0, 0, 0, 0, 0, 0, 0, 0);    // 2000
    private static readonly AddCashCountRequest EmptyDrawer = new(0, 0, 0, 0, 0, 0, 0, 0, 0);    // 0

    private static AddCashCountCommandHandler AddHandler(CashDrawerTestContext c) =>
        new(c.CountRepository(), c.Clock, c.StoreIdentity);

    private static GetCashCountsQueryHandler GetHandler(CashDrawerTestContext c) =>
        new(c.CountRepository(), c.Clock, c.StoreIdentity);

    [Fact]
    public async Task Add_WithANegativeDenomination_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var negative = MorningCount with { Coin5Count = -1 };

        var act = () => AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, negative));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Add_WithANegativeDenomination_WritesNothing()
    {
        await using var c = new CashDrawerTestContext();
        var negative = MorningCount with { BankNote1000Count = -3 };

        try { await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, negative)); }
        catch (CashEntryValidationException) { }

        c.Db.CashCounts.Should()
                       .BeEmpty();
    }

    [Fact]
    public async Task Add_WithANegativeDenomination_WritesNoEvent()
    {
        await using var c = new CashDrawerTestContext();
        var negative = MorningCount with { Coin2Count = -1 };

        try { await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, negative)); }
        catch (CashEntryValidationException) { }

        c.OutboxEvents().Should()
                        .BeEmpty();
    }

    [Fact]
    public async Task Add_WithAllZeroCounts_ReturnsZeroTotal()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, EmptyDrawer));

        result.CountedTotal.Should()
                           .Be(0m);
    }

    [Fact]
    public async Task Add_WhenSecondCountSameDay_KeepsBothRows()
    {
        await using var c = new CashDrawerTestContext();
        await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));
        c.Time.Advance(TimeSpan.FromHours(10));

        await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.OtherCashierId, EveningCount));

        c.Db.CashCounts.Should()
                       .HaveCount(2);
    }

    [Fact]
    public async Task Add_WithValidCounts_ReturnsTheCountedTotal()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));

        result.CountedTotal.Should()
                           .Be(1603m);
    }

    [Fact]
    public async Task Add_WithValidCounts_RecordsWhoCounted()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));

        result.CreatedByUserId.Should()
                              .Be(CashDrawerTestContext.CashierId);
    }

    [Fact]
    public async Task Add_WithValidCounts_WritesACountChangedEvent()
    {
        await using var c = new CashDrawerTestContext();

        await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));

        c.OutboxEvents().Should()
                        .ContainSingle(e => e.Type == CashDrawerOutbox.CashCountChanged);
    }

    [Fact]
    public async Task Get_WithNoCountsToday_ReturnsEmpty()
    {
        await using var c = new CashDrawerTestContext();

        var result = await GetHandler(c).HandleAsync(new GetCashCountsQuery(null));

        result.Should()
              .BeEmpty();
    }

    [Fact]
    public async Task Get_WithPastDate_ExcludesTodaysCounts()
    {
        await using var c = new CashDrawerTestContext();
        await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));

        var result = await GetHandler(c).HandleAsync(new GetCashCountsQuery(CashDrawerTestContext.Yesterday));

        result.Should()
              .BeEmpty();
    }

    [Fact]
    public async Task Get_WithTwoCountsToday_ReturnsNewestFirst()
    {
        await using var c = new CashDrawerTestContext();
        var morning = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));
        c.Time.Advance(TimeSpan.FromHours(10));
        var evening = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.OtherCashierId, EveningCount));

        var result = await GetHandler(c).HandleAsync(new GetCashCountsQuery(null));

        result.Select(r => r.Id).Should()
                                .Equal(evening.Id, morning.Id);
    }
}
