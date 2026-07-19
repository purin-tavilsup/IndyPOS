using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class StoreTypeFeaturesTests
{
    [Fact]
    public void For_GeneralHardware_EnablesBothFeatures()
    {
        var f = StoreTypeFeatures.For(StoreType.GeneralHardware);
        f.PayLaterEnabled.Should().BeTrue();
        f.MultipleProductTypesEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(StoreType.Minimart)]
    [InlineData(StoreType.CoffeeShop)]
    public void For_GeneralOnlyStoreTypes_DisableBothFeatures(StoreType storeType)
    {
        var f = StoreTypeFeatures.For(storeType);
        f.PayLaterEnabled.Should().BeFalse();
        f.MultipleProductTypesEnabled.Should().BeFalse();
    }
}
