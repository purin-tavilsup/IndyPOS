using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Summary;

public class CashDrawerCalculatorTests
{
    private static readonly CashDrawerTotals Day = new(
        CashPaymentsTotal: 10_000m,
        DebtRepaymentsTotal: 300m,
        CashFloatsTotal: 1_000m,
        PayoutsTotal: 650m);

    // A payment stores what the customer handed over; the change leaves the drawer (memory: Payment.Amount is tendered).
    [Fact]
    public void CashKept_WithChange_SubtractsIt()
    {
        var result = CashDrawerCalculator.CashKept(80m, [new SalePayment("Cash", 100m)]);

        result.Should()
              .Be(80m);
    }

    [Fact]
    public void CashKept_WithAMixedBill_SubtractsTheChangeFromCash()
    {
        var result = CashDrawerCalculator.CashKept(100m, [new SalePayment("MoneyTransfer", 60m), new SalePayment("Cash", 50m)]);

        result.Should()
              .Be(40m);
    }

    // Paid back less than the refund's total, so "payments - total" is +20: only the refund rule keeps it from
    // being taken off again as change (review finding: a refund paid exactly pinned nothing).
    [Fact]
    public void CashKept_WithARefund_KeepsNoChange()
    {
        var result = CashDrawerCalculator.CashKept(-100m, [new SalePayment("Cash", -80m)]);

        result.Should()
              .Be(-80m);
    }

    [Fact]
    public void CashKept_WithALowerCaseCashCode_CountsIt()
    {
        var result = CashDrawerCalculator.CashKept(50m, [new SalePayment("cash", 50m)]);

        result.Should()
              .Be(50m);
    }

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
        var result = CashDrawerCalculator.ExpectedCash(new CashDrawerTotals(0m, 0m, 0m, 50m));

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
    public void ExpectedCash_WithATypicalDay_ReturnsCashPlusRepaidPlusFloatsMinusPayouts()
    {
        var result = CashDrawerCalculator.ExpectedCash(Day);

        result.Should()
              .Be(10_000m + 300m + 1_000m - 650m);
    }

    [Fact]
    public void Difference_WhenCountIsOver_ReturnsPositive()
    {
        var result = CashDrawerCalculator.Difference(countedCash: 120m, expectedCash: 100m);

        result.Should()
              .Be(20m);
    }
}
