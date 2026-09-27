using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Summary;

public class CashDrawerCalculatorTests
{
    private static readonly CashDrawerTotals Day = new(
        CashSalesTotal: 10_000m,
        DebtRepaymentsTotal: 300m,
        CashFloatsTotal: 1_000m,
        MoneyTransferTotal: 2_500m,
        WelfareCardTotal: 400m,
        PayoutsTotal: 650m);

    [Fact]
    public void Difference_WithNoCount_ReturnsNull()
    {
        var result = CashDrawerCalculator.Difference(countedCash: null, expectedCash: 100m);

        result.Should()
              .BeNull();
    }

    [Fact]
    public void Difference_WhenCountIsShort_ReturnsNegative()
    {
        var result = CashDrawerCalculator.Difference(countedCash: 90m, expectedCash: 100m);

        result.Should()
              .Be(-10m);
    }

    [Fact]
    public void ExpectedCash_WithPayoutsAboveTakings_ReturnsNegative()
    {
        var result = CashDrawerCalculator.ExpectedCash(new CashDrawerTotals(0m, 0m, 0m, 0m, 0m, 50m));

        result.Should()
              .Be(-50m);
    }

    [Fact]
    public void ExpectedCash_WithAllZero_ReturnsZero()
    {
        var result = CashDrawerCalculator.ExpectedCash(default);

        result.Should()
              .Be(0m);
    }

    [Fact]
    public void ExpectedCash_WithATypicalDay_ReturnsTodaysFormula()
    {
        var result = CashDrawerCalculator.ExpectedCash(Day);

        result.Should()
              .Be(10_000m + 300m + 1_000m - 2_500m - 400m - 650m);
    }

    [Fact]
    public void Difference_WhenCountIsOver_ReturnsPositive()
    {
        var result = CashDrawerCalculator.Difference(countedCash: 120m, expectedCash: 100m);

        result.Should()
              .Be(20m);
    }
}
