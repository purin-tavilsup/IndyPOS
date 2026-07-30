using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using Xunit;

namespace IndyPOS.Domain.Tests.ValueObjects;

public class ProductCategoryPolicyTests
{
    [Fact]
    public void IsUsable_WithGeneralGoods_ShouldAlwaysBeTrue()
    {
        // Every store sells general goods; this is the one kind with no gate.
        foreach (var storeType in Enum.GetValues<StoreType>())
        {
            ProductCategoryPolicy
                .IsUsable(ProductCategoryKind.GeneralGoods, StoreTypeFeatures.For(storeType))
                .Should().BeTrue($"{storeType} must be able to sell general goods");
        }
    }

    [Fact]
    public void IsUsable_WithHardwareOnAGeneralOnlyStore_ShouldBeFalse()
    {
        var features = StoreTypeFeatures.For(StoreType.Minimart);

        ProductCategoryPolicy.IsUsable(ProductCategoryKind.Hardware, features).Should().BeFalse();
    }

    [Fact]
    public void IsUsable_WithHardwareOnAMultiTypeStore_ShouldBeTrue()
    {
        var features = StoreTypeFeatures.For(StoreType.GeneralHardware);

        ProductCategoryPolicy.IsUsable(ProductCategoryKind.Hardware, features).Should().BeTrue();
    }

    [Fact]
    public void IsUsable_WithServiceOnAGeneralOnlyStore_ShouldBeFalse()
    {
        // Services are a non-general kind, so the same gate applies. MimyShop seeds a
        // Services category but cannot use it until the feature exists - see the spec's
        // "Known gap" section. Failing closed is the safe direction.
        var features = StoreTypeFeatures.For(StoreType.Minimart);

        ProductCategoryPolicy.IsUsable(ProductCategoryKind.Service, features).Should().BeFalse();
    }
}
