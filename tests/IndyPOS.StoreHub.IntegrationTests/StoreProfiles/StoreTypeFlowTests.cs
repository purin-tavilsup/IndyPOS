using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

/// <summary>
/// The flows that differ by store type, once per dev store. Every expected value is written here,
/// never read from the code under test, so a policy or seed change that drops one fails.
/// </summary>
[Collection(StoreProfilesCollection.Name)]
public class StoreTypeFlowTests(StoreProfileHosts hosts)
{
    private const string PayLater = "PayLater";
    private const string Cash = "Cash";

    public static TheoryData<string, string[]> OfferedMethods => new()
    {
        { "GeneralHardware", ["Cash", "MoneyTransfer", "WelfareCard", "PayLater"] },
        { "MimyMart", ["Cash", "MoneyTransfer", "WelfareCard"] },
        { "MimyShop", ["Cash", "MoneyTransfer", "WelfareCard"] }
    };

    public static TheoryData<string, string[]> Categories => new()
    {
        { "GeneralHardware", ["Miscellaneous", "Beverages", "Snacks", "AlcoholicBeverages", "Food", "Stationery",
                              "Household", "ElectricalAppliances", "Toys", "Medicine", "Agriculture",
                              "GeneralMaterials", "MaterialsAndEquipment", "PlumbingMaterials",
                              "ElectricalMaterials", "ConstructionMaterials"] },
        { "MimyMart", ["Miscellaneous", "Beverages", "Snacks", "AlcoholicBeverages", "Food", "Stationery",
                       "Household", "ElectricalAppliances", "Toys", "Medicine", "Agriculture"] },
        { "MimyShop", ["Gifts", "Toys", "Stationery", "BooksAndNotebooks", "Cosmetics", "Jewellery", "Bags",
                       "Fashion", "Household", "Kitchenware", "SnacksAndBeverages", "MobileAccessories",
                       "Electronics", "PartySupplies", "SeasonalGoods", "Services", "Miscellaneous"] }
    };

    [Theory]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public async Task PayLaterSale_WithAStoreWithoutPayLater_ReturnsBadRequest(string key)
    {
        var client = await hosts.SignedInAsync(key);

        var response = await client.PostAsJsonAsync("/sales", await SaleAsync(key, PayLater, note: "ลูกค้าทดสอบ"));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
        (await ErrorOfAsync(response)).Should()
                                      .Be("ช่องทางชำระเงิน 'PayLater' ใช้กับร้านนี้ไม่ได้");
    }

    [Fact]
    public async Task PayLaterSale_OnGeneralHardware_CreatesTheDebt()
    {
        var client = await hosts.SignedInAsync("GeneralHardware");

        var response = await client.PostAsJsonAsync("/sales", await SaleAsync("GeneralHardware", PayLater, note: "ลูกค้าทดสอบ"));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    [Theory]
    [MemberData(nameof(OfferedMethods))]
    public async Task OfferedPaymentMethods_ForEachStore_MatchTheStoresSet(string key, string[] expected)
    {
        var client = await hosts.SignedInAsync(key);

        var methods = await client.GetFromJsonAsync<List<PaymentMethodDto>>("/payment-methods");

        methods!.Select(m => m.Code).Should()
                                    .Equal(expected);
    }

    [Theory]
    [MemberData(nameof(Categories))]
    public async Task ProductCategories_ForEachStore_MatchTheStoreType(string key, string[] expected)
    {
        var client = await hosts.SignedInAsync(key);

        var categories = await client.GetFromJsonAsync<List<ProductCategoryDto>>("/product-categories");

        categories!.Select(c => c.Code).Should()
                                       .BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("GeneralHardware", true, true)]
    [InlineData("MimyMart", false, false)]
    [InlineData("MimyShop", false, false)]
    public async Task StoreFeatures_ForEachStore_ReportTheTypesFeatures(string key, bool payLater, bool multipleTypes)
    {
        var client = await hosts.SignedInAsync(key);

        var features = await client.GetFromJsonAsync<StoreFeaturesDto>("/store/features");

        features.Should()
                .Be(new StoreFeaturesDto(payLater, multipleTypes));
    }

    [Theory]
    [InlineData("GeneralHardware")]
    [InlineData("MimyMart")]
    [InlineData("MimyShop")]
    public async Task CashSale_ForEachStore_Completes(string key)
    {
        var client = await hosts.SignedInAsync(key);

        var response = await client.PostAsJsonAsync("/sales", await SaleAsync(key, Cash));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    // A service (จัดส่ง) is sold without moving stock: there is none to move.
    [Fact]
    public async Task ServiceSale_OnMimyShop_MovesNoStock()
    {
        var client = await hosts.SignedInAsync("MimyShop");
        var delivery = await ProductAsync("MimyShop", "2002500000014");
        var before = await MovementsOfAsync("MimyShop", delivery.Id);

        var response = await client.PostAsJsonAsync("/sales", new CompleteSaleRequest(
            Lines: [new SaleLineRequest(delivery.Id, Quantity: 1, UnitPrice: delivery.UnitPrice)],
            Payments: [new SalePaymentRequest(Cash, delivery.UnitPrice)]));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
        (await MovementsOfAsync("MimyShop", delivery.Id)).Should()
                                                         .Be(before);
    }

    private async Task<CompleteSaleRequest> SaleAsync(string key, string method, string? note = null)
    {
        var trackable = Profiles.Find(key)!.Products.First(p => p.IsTrackable);
        var product = await ProductAsync(key, trackable.Barcode);
        return new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: product.UnitPrice)],
            Payments: [new SalePaymentRequest(method, product.UnitPrice, note)]);
    }

    private async Task<IndyPOS.Domain.Entities.Core.Product> ProductAsync(string key, string barcode)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Products
                          .AsNoTracking().SingleAsync(p => p.Barcode == barcode);
    }

    private async Task<int> MovementsOfAsync(string key, Guid productId)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>()
                          .Set<IndyPOS.Domain.Entities.Core.InventoryMovement>()
                          .CountAsync(m => m.ProductId == productId);
    }

    private static async Task<string?> ErrorOfAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();
}
