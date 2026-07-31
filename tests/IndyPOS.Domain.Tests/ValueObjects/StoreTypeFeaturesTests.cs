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
    [InlineData(StoreType.MimyShop)]
    public void For_GeneralOnlyStoreTypes_DisableBothFeatures(StoreType storeType)
    {
        var f = StoreTypeFeatures.For(storeType);
        f.PayLaterEnabled.Should().BeFalse();
        f.MultipleProductTypesEnabled.Should().BeFalse();
    }

    [Fact]
    public void For_WithMimyShop_ShouldMatchMinimart()
    {
        // MimyShop differs from a minimart only in its seeded category set today. Modelling it
        // as a real store type is deliberate (services and reporting will diverge), so this
        // test pins that the flags are intentionally identical rather than accidentally copied.
        StoreTypeFeatures.For(StoreType.MimyShop)
            .Should().BeEquivalentTo(StoreTypeFeatures.For(StoreType.Minimart));
    }

    [Fact]
    public void StoreType_ShouldNotDefineCoffeeShop()
    {
        // Removed 2026-07-29: coffee shops need a dedicated app. Value 3 stays reserved so a
        // future type cannot silently inherit persisted CoffeeShop rows.
        Enum.GetNames<StoreType>().Should().NotContain("CoffeeShop");
        Enum.IsDefined(typeof(StoreType), 3).Should().BeFalse();
    }
}
