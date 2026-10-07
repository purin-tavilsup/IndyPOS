using FluentAssertions;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

[Collection(StoreProfilesCollection.Name)]
public class DevSeedTests(StoreProfileHosts hosts)
{
    private const string InitialStock = "InitialStock";

    // Aspire restarts reuse the store's database: re-seeding must not double the opening stock.
    [Fact]
    public async Task DevSeed_RunTwice_AddsInitialStockOnce()
    {
        var services = hosts.ServicesFor("MimyMart");

        await StoreProfileHosts.SeedLikeDevelopmentAsync(services);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var openingRows = await db.Set<IndyPOS.Domain.Entities.Core.InventoryMovement>()
                                  .CountAsync(m => m.Reason == InitialStock);
        openingRows.Should()
                   .Be(Profiles.MimyMart.Products.Count(p => p.IsTrackable && p.InitialStock > 0));
    }

    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public async Task DevSeed_ForEachStore_SeedsThatStoresProducts(string key)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var barcodes = await db.Products.Select(p => p.Barcode).ToListAsync();

        barcodes.Should()
                .BeEquivalentTo(Profiles.Find(key)!.Products.Select(p => p.Barcode));
    }
}
