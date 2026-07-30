using System.Reflection;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Constants;

public class ProductCategoryCodesTests
{
    private static IReadOnlyList<FieldInfo> Constants() =>
        typeof(ProductCategoryCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false })
            .ToList();

    [Fact]
    public void EveryConstant_ShouldHaveAValueEqualToItsName()
    {
        // The code IS the identifier. A mismatch means a rename silently changed stored data.
        foreach (var field in Constants())
        {
            field.GetValue(null).Should().Be(field.Name);
        }
    }

    [Fact]
    public void Codes_ShouldBeUnique()
    {
        var values = Constants().Select(f => (string)f.GetValue(null)!).ToList();

        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Codes_ShouldCoverEveryCategoryTheThreeStoresUse()
    {
        // 16 GeneralHardware + 17 MimyShop, minus the 4 codes shared between them
        // (Toys, Stationery, Household, Miscellaneous). MimyMart's 10 are a subset of
        // GeneralHardware's. See the spec's section 4 tables.
        Constants().Should().HaveCount(29);
    }

    [Fact]
    public void Codes_ShouldIncludeTheServicesCategory()
    {
        ProductCategoryCodes.Services.Should().Be("Services");
    }
}
