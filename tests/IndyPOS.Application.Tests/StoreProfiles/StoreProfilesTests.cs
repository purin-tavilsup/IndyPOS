using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.StoreProfiles;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.Application.Tests.StoreProfiles;

public class StoreProfilesTests
{
    private const string UnknownKey = "NoSuchStore";

    [Fact]
    public void Resolve_WithAnUnknownKey_ThrowsListingTheValidKeys()
    {
        var act = () => Profiles.Resolve(UnknownKey);

        act.Should()
           .Throw<ArgumentException>()
           .WithMessage("*GeneralHardware*MimyMart*MimyShop*all*");
    }

    [Fact]
    public void Find_WithAnUnknownKey_ReturnsNull()
    {
        Profiles.Find(UnknownKey).Should()
                                 .BeNull();
    }

    [Theory]
    [InlineData("mimymart")]
    [InlineData(" MimyMart ")]
    [InlineData("MIMYMART")]
    public void Resolve_WithAKeyInAnyCase_ReturnsThatStore(string selection)
    {
        Profiles.Resolve(selection).Select(p => p.Key).Should()
                                                      .Equal("MimyMart");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_WithNoSelection_ReturnsGeneralHardware(string? selection)
    {
        Profiles.Resolve(selection).Select(p => p.Key).Should()
                                                      .Equal("GeneralHardware");
    }

    [Fact]
    public void Resolve_WithAll_ReturnsEveryStore()
    {
        Profiles.Resolve("ALL").Select(p => p.Key).Should()
                                                  .Equal("GeneralHardware", "MimyMart", "MimyShop");
    }

    [Fact]
    public void All_ForEachStoreType_HasExactlyOneProfile()
    {
        Profiles.All.Select(p => p.Type).Should()
                                         .BeEquivalentTo(Enum.GetValues<StoreType>());
    }

    [Fact]
    public void All_WithTheirDevPorts_UsesEachPortOnceAndKeeps5012ForGeneralHardware()
    {
        Profiles.All.Select(p => p.DevPort).Should()
                                            .OnlyHaveUniqueItems();
        Profiles.Default.DevPort.Should()
                                .Be(5012);
    }

    [Fact]
    public void MimyShop_ServiceProducts_UseTheRealBarcodesAndMoveNoStock()
    {
        var services = Profiles.Find("MimyShop")!.Products.Where(p => p.Category == ProductCategoryCodes.Services);

        services.Select(p => (p.Barcode, p.IsTrackable)).Should()
                                                        .BeEquivalentTo([("2002500000014", false), ("2002500000021", false)]);
    }

    // The sale panel's เบ็ดเตล็ด button opens this product; without it the dev till shows ไม่พบสินค้าในระบบ.
    // Every real store has it, untracked, at price 0: the cashier types the price.
    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public void Products_ForEachStore_IncludeTheGeneralGoodsTemplate(string key)
    {
        var template = Profiles.Find(key)!.Products.SingleOrDefault(p => p.Barcode == TemplateProductBarcodes.GeneralGoods);

        template.Should()
                .Be(new StoreProfileProduct(TemplateProductBarcodes.GeneralGoods, "สินค้าเบ็ดเตล็ด",
                                            ProductCategoryCodes.Miscellaneous, UnitPrice: 0m, InitialStock: 0,
                                            IsTrackable: false));
    }

    // Behind GeneralHardware's ฮาร์ดแวร์ button. Its legacy category was วัสดุและอุปกรณ์ทั่วไป.
    [Fact]
    public void GeneralHardware_Products_IncludeTheHardwareTemplate()
    {
        var template = Profiles.Find("GeneralHardware")!.Products.SingleOrDefault(p => p.Barcode == TemplateProductBarcodes.Hardware);

        template.Should()
                .Be(new StoreProfileProduct(TemplateProductBarcodes.Hardware, "สินค้าฮาร์ดแวร์",
                                            ProductCategoryCodes.GeneralMaterials, UnitPrice: 0m, InitialStock: 0,
                                            IsTrackable: false));
    }

    // A misspelt code would leave that method switched off with no error.
    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public void PaymentMethods_ForEachStore_AreCatalogueCodes(string key)
    {
        var catalogueCodes = typeof(IndyPOS.Application.Common.Constants.PaymentMethodCodes)
                             .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                             .Where(f => f.IsLiteral)
                             .Select(f => (string)f.GetRawConstantValue()!);

        Profiles.Find(key)!.PaymentMethods.Should()
                                          .BeSubsetOf(catalogueCodes);
    }

    [Fact]
    public void PaymentMethods_OfStoresOtherThanGeneralHardware_NeverIncludePayLater()
    {
        var withPayLater = Profiles.All.Where(p => p.PaymentMethods.Contains("PayLater")).Select(p => p.Key);

        withPayLater.Should()
                    .Equal("GeneralHardware");
    }

    // A product filed under a code the store type does not seed is invisible to every picker.
    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public void Products_ForEachStore_UseOnlyCategoriesThatStoreTypeSeeds(string key)
    {
        var profile = Profiles.Find(key)!;

        profile.Products.Select(p => p.Category).Should()
                                                .BeSubsetOf(ProductCategorySeeder.CodesFor(profile.Type));
    }
}
