using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
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
        var services = Profiles.Find("MimyShop")!.Products.Where(p => !p.IsTrackable);

        services.Select(p => p.Barcode).Should()
                                       .BeEquivalentTo(["2002500000014", "2002500000021"]);
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
