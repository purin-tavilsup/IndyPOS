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

    [Theory]
    [InlineData(StoreType.GeneralHardware, false)]
    [InlineData(StoreType.Minimart, false)]
    [InlineData(StoreType.MimyShop, true)]
    public void For_EachStoreType_EnablesServiceProductsOnlyForMimyShop(StoreType storeType, bool expected)
    {
        StoreTypeFeatures.For(storeType).ServiceProductsEnabled.Should()
                                                               .Be(expected);
    }

    // The sale panel puts the service buttons in the hardware button's slot.
    [Fact]
    public void For_EachStoreType_NeverEnablesHardwareAndServicesTogether()
    {
        var both = Enum.GetValues<StoreType>()
                       .Select(StoreTypeFeatures.For)
                       .Where(f => f.ServiceProductsEnabled && f.MultipleProductTypesEnabled);

        both.Should()
            .BeEmpty();
    }

    [Fact]
    public void For_WithMimyShop_DiffersFromMinimartOnlyInServiceProducts()
    {
        // Modelling MimyShop as its own store type was deliberate: it sells services. Every other
        // flag must stay identical to a minimart's, so this pins that the difference is just that one.
        StoreTypeFeatures.For(StoreType.MimyShop).Should()
                                                 .Be(StoreTypeFeatures.For(StoreType.Minimart) with { ServiceProductsEnabled = true });
    }

    [Fact]
    public void ServiceProductBarcodes_All_AreMimyShopsRealBarcodes()
    {
        ServiceProductBarcodes.All.Should()
                                  .Equal("2002500000014", "2002500000021");
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
