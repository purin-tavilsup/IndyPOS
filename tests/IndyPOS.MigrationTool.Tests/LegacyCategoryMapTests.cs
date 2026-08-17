using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Guards the seam between a store-agnostic map and a store-type-scoped catalogue.
/// </summary>
/// <remarks>
/// <see cref="LegacyCategoryMap"/> is deliberately keyed on the Thai NAME and knows nothing about
/// which store it is resolving for, because legacy ids mean different things in different stores.
/// <see cref="ProductCategorySeeder"/>, in contrast, seeds a different catalogue per store type. So
/// the map's output is NOT a subset of any one store's catalogue by construction, and nothing at
/// runtime notices: <c>Product.Category</c> is a plain string with no foreign key, and
/// <c>MigrationVerifier</c> derives its expectation from the same map, so it agrees with a wrong
/// answer and reports green.
/// <para>
/// This is the one oracle that is genuinely independent of the mapping, which is why it is a test
/// rather than a runtime check: the catalogue is empty in the target database at migration time
/// (StoreHub seeds it at startup), so the migrator cannot validate against it and be sure.
/// </para>
/// <para>
/// Contrast <c>LegacyPaymentTypeMap</c>, which the category work is modelled on: that analogy holds
/// in form but not here, because <c>PaymentMethodSeeder</c> seeds all seven codes for EVERY store,
/// making its map a subset by construction. This one has no such guarantee.
/// </para>
/// </remarks>
public class LegacyCategoryMapTests
{
    [Theory]
    [InlineData(StoreType.GeneralHardware)]
    [InlineData(StoreType.Minimart)]
    [InlineData(StoreType.MimyShop)]
    public void ToCode_ForEveryCategoryAStoreCanPresent_ReturnsACodeThatStoreTypeSeeds(StoreType storeType)
    {
        var seeded = ProductCategorySeeder.CodesFor(storeType);

        var unusable = LegacyCategoryLookup.NamesFor(storeType)
            .Select(name => new { Name = name, Code = LegacyCategoryMap.ToCode(name) })
            .Where(resolved => resolved.Code is null || !seeded.Contains(resolved.Code))
            .Select(resolved => $"{resolved.Name} -> {resolved.Code ?? "(no code)"}")
            .ToList();

        unusable.Should().BeEmpty(
            "a {0} store can file a product under any of these, and migrating it to a code {0} does " +
            "not seed leaves the product uneditable and invisible to every category picker",
            storeType);
    }
}
