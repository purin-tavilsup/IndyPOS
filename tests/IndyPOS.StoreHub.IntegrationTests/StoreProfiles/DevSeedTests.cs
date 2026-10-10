using FluentAssertions;
using IndyPOS.Domain.ValueObjects;
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

    // A seed that died between saving a product and its opening stock left the product at 0 for good:
    // re-seeding skipped every existing product.
    [Fact]
    public async Task DevSeed_WithAProductMissingItsOpeningStock_RestoresItOnce()
    {
        var services = hosts.ServicesFor("GeneralHardware");
        var cement = Profiles.GeneralHardware.Products[0];
        var productId = await DeleteOpeningStockOfAsync(services, cement.Barcode);

        await StoreProfileHosts.SeedLikeDevelopmentAsync(services);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var opening = await db.InventoryMovements.Where(m => m.ProductId == productId && m.Reason == InitialStock)
                                                 .Select(m => m.QuantityDelta)
                                                 .ToListAsync();
        opening.Should()
               .Equal(cement.InitialStock);
    }

    // A dev database can already hold a template barcode as a trackable product (added by hand while the
    // seed lacked it). The refresh must make it untracked, or every เบ็ดเตล็ด sale moves stock below zero.
    [Fact]
    public async Task DevSeed_WithATemplateProductSavedAsTrackable_MakesItUntracked()
    {
        var services = hosts.ServicesFor("MimyMart");
        await SetTrackableAsync(services, TemplateProductBarcodes.GeneralGoods, isTrackable: true);

        await StoreProfileHosts.SeedLikeDevelopmentAsync(services);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var isTrackable = await db.Products.Where(p => p.Barcode == TemplateProductBarcodes.GeneralGoods)
                                           .Select(p => p.IsTrackable)
                                           .SingleAsync();
        isTrackable.Should()
                   .BeFalse();
    }

    private static async Task SetTrackableAsync(IServiceProvider services, string barcode, bool isTrackable)
    {
        await StoreProfileHosts.SeedLikeDevelopmentAsync(services);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        await db.Products.Where(p => p.Barcode == barcode)
                         .ExecuteUpdateAsync(set => set.SetProperty(p => p.IsTrackable, isTrackable));
    }

    private static async Task<Guid> DeleteOpeningStockOfAsync(IServiceProvider services, string barcode)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var productId = await db.Products.Where(p => p.Barcode == barcode).Select(p => p.Id).SingleAsync();
        await db.InventoryMovements.Where(m => m.ProductId == productId && m.Reason == InitialStock).ExecuteDeleteAsync();
        return productId;
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
