using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Xunit;

namespace IndyPOS.Application.Tests.Infrastructure;

public class PaymentMethodTotalsTests
{
    private static readonly PaymentMethod Cash = Method("Cash", "เงินสด", enabled: true, order: 1);
    private static readonly PaymentMethod Transfer = Method("MoneyTransfer", "เงินโอน", enabled: true, order: 2);
    private static readonly PaymentMethod WeWin = Method("WeWin", "เราชนะ", enabled: false, order: 7);

    [Fact]
    public void Build_WithADisabledMethodAndNoSales_LeavesItOut()
    {
        var totals = PaymentMethodTotals.Build([Cash, WeWin], [Pay("Cash", 100m)]);

        totals.Select(t => t.Code).Should()
                                  .Equal("Cash");
    }

    [Fact]
    public void Build_WithADisabledMethodThatHadSales_ListsIt()
    {
        var totals = PaymentMethodTotals.Build([Cash, WeWin], [Pay("WeWin", 40m)]);

        totals.Should()
              .Contain(new PaymentMethodTotalDto("WeWin", "เราชนะ", 40m));
    }

    // A sale accepts a method code in any case and stores the caller's spelling, so "cash" is Cash.
    [Fact]
    public void Build_WithADifferentlyCasedCode_CountsItUnderTheCatalogueMethod()
    {
        var totals = PaymentMethodTotals.Build([Cash], [Pay("cash", 100m), Pay("Cash", 20m)]);

        totals.Should()
              .Equal(new PaymentMethodTotalDto("Cash", "เงินสด", 120m));
    }

    [Fact]
    public void Build_WithACodeOutsideTheCatalogue_ListsItLastUnderItsCode()
    {
        var totals = PaymentMethodTotals.Build([Cash], [Pay("Card", 25m), Pay("Cash", 10m)]);

        totals.Should()
              .Equal(new PaymentMethodTotalDto("Cash", "เงินสด", 10m), new PaymentMethodTotalDto("Card", "Card", 25m));
    }

    [Fact]
    public void Build_WithAnEnabledMethodAndNoSales_ListsItAtZero()
    {
        var totals = PaymentMethodTotals.Build([Cash, Transfer], []);

        totals.Should()
              .Equal(new PaymentMethodTotalDto("Cash", "เงินสด", 0m), new PaymentMethodTotalDto("MoneyTransfer", "เงินโอน", 0m));
    }

    [Fact]
    public void Build_WithMethodsOutOfOrder_OrdersByDisplayOrder()
    {
        var totals = PaymentMethodTotals.Build([Transfer, Cash], []);

        totals.Select(t => t.Code).Should()
                                  .Equal("Cash", "MoneyTransfer");
    }

    [Fact]
    public void Build_WithSeveralPaymentsOfOneMethod_SumsThem()
    {
        var totals = PaymentMethodTotals.Build([Cash], [Pay("Cash", 100m), Pay("Cash", 50.5m)]);

        totals.Single().Total.Should()
                             .Be(150.5m);
    }

    private static PaymentMethod Method(string code, string name, bool enabled, int order) =>
        new() { Code = code, DisplayName = name, IsEnabled = enabled, DisplayOrder = order, StoreId = "TEST" };

    private static Payment Pay(string method, decimal amount) =>
        new() { Id = Guid.NewGuid(), Method = method, Amount = amount };
}
