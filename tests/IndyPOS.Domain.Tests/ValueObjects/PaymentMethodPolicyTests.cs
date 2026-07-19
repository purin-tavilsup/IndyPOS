using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class PaymentMethodPolicyTests
{
    private static PaymentMethod Method(string code, bool enabled = true, int order = 0) => new()
    {
        Code = code, DisplayName = code, Kind = PaymentMethodKind.Permanent,
        IsEnabled = enabled, DisplayOrder = order, StoreId = "s"
    };

    [Fact]
    public void Offerable_ForGeneralHardware_ShouldIncludePayLater()
    {
        var methods = new[] { Method("Cash", order: 1), Method("PayLater", order: 2) };

        var result = PaymentMethodPolicy.Offerable(methods, StoreType.GeneralHardware);

        result.Select(m => m.Code).Should().ContainInOrder("Cash", "PayLater");
    }

    [Theory]
    [InlineData(StoreType.Minimart)]
    [InlineData(StoreType.CoffeeShop)]
    public void Offerable_ForNonGeneralHardware_ShouldExcludePayLaterEvenWhenEnabled(StoreType storeType)
    {
        var methods = new[] { Method("Cash"), Method("PayLater", enabled: true) };

        var result = PaymentMethodPolicy.Offerable(methods, storeType);

        result.Select(m => m.Code).Should().NotContain("PayLater");
        result.Select(m => m.Code).Should().Contain("Cash");
    }

    [Fact]
    public void Offerable_ShouldExcludeDisabledMethods()
    {
        var methods = new[] { Method("Cash"), Method("M33WeLove", enabled: false) };

        var result = PaymentMethodPolicy.Offerable(methods, StoreType.GeneralHardware);

        result.Select(m => m.Code).Should().NotContain("M33WeLove");
    }

    [Fact]
    public void Offerable_ShouldOrderByDisplayOrder()
    {
        var methods = new[] { Method("MoneyTransfer", order: 3), Method("Cash", order: 1) };

        var result = PaymentMethodPolicy.Offerable(methods, StoreType.GeneralHardware);

        result.Select(m => m.Code).Should().ContainInOrder("Cash", "MoneyTransfer");
    }
}
