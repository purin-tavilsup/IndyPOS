using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.ValueObjects;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

/// <summary>
/// Expected cash per store, through real sales. The hosts' databases are shared across this
/// collection, so each test measures a before/after difference or a code list, never an absolute total.
/// </summary>
[Collection(StoreProfilesCollection.Name)]
public class CashSummaryPerStoreTests(StoreProfileHosts hosts)
{
    private const decimal DeliveryPrice = 30m;
    private const decimal HundredNote = 100m;

    [Fact]
    public async Task CashSummary_ForMimyShopAfterAFiftyFiftySale_KeepsExpectedCash()
    {
        var before = await SummaryAsync("MimyShop");

        await SellDeliveryAsync("MimyShop", "FiftyFifty");
        var after = await SummaryAsync("MimyShop");

        after.ExpectedCash.Should()
                          .Be(before.ExpectedCash);
    }

    [Fact]
    public async Task CashSummary_ForMimyShopAfterAFiftyFiftySale_AddsItToItsNonCashLine()
    {
        var before = await SummaryAsync("MimyShop");

        await SellDeliveryAsync("MimyShop", "FiftyFifty");
        var after = await SummaryAsync("MimyShop");

        NonCashTotal(after, "FiftyFifty").Should()
                                         .Be(NonCashTotal(before, "FiftyFifty") + DeliveryPrice);
    }

    [Fact]
    public async Task CashSummary_ForMimyMart_ListsOnlyMoneyTransferAsNonCash()
    {
        var summary = await SummaryAsync("MimyMart");

        summary.NonCashPayments.Select(p => p.Code).Should()
                                                   .Equal("MoneyTransfer");
    }

    [Fact]
    public async Task CashSummary_ForGeneralHardware_HasNoPayLaterNonCashLine()
    {
        var summary = await SummaryAsync("GeneralHardware");

        summary.NonCashPayments.Should()
                               .NotContain(p => p.Code == "PayLater");
    }

    [Fact]
    public async Task CashSummary_ForMimyShopAfterACashSale_AddsItToExpectedCash()
    {
        var before = await SummaryAsync("MimyShop");

        await SellDeliveryAsync("MimyShop", "Cash");
        var after = await SummaryAsync("MimyShop");

        after.ExpectedCash.Should()
                          .Be(before.ExpectedCash + DeliveryPrice);
    }

    [Fact]
    public async Task CashSummary_ForMimyShopAfterACashSaleWithChange_AddsOnlyTheBill()
    {
        var before = await SummaryAsync("MimyShop");

        await SellDeliveryAsync("MimyShop", "Cash", tendered: HundredNote);
        var after = await SummaryAsync("MimyShop");

        after.ExpectedCash.Should()
                          .Be(before.ExpectedCash + DeliveryPrice);
    }

    private static decimal NonCashTotal(CashDrawerSummaryDto summary, string code) =>
        summary.NonCashPayments.Single(p => p.Code == code).Total;

    // No businessDate: the server picks the store's own today, so the test PC's timezone cannot matter.
    private async Task<CashDrawerSummaryDto> SummaryAsync(string key)
    {
        var client = await hosts.SignedInAsync(key);
        return (await client.GetFromJsonAsync<CashDrawerSummaryDto>("/cash/summary"))!;
    }

    private async Task SellDeliveryAsync(string key, string method, decimal tendered = DeliveryPrice)
    {
        var client = await hosts.SignedInAsync(key);
        var product = await ProductAsync(key, ServiceProductBarcodes.Delivery);
        var response = await client.PostAsJsonAsync("/sales", new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: DeliveryPrice)],
            Payments: [new SalePaymentRequest(method, tendered)]));
        response.EnsureSuccessStatusCode();
    }

    private async Task<Product> ProductAsync(string key, string barcode)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Products
                          .AsNoTracking()
                          .SingleAsync(p => p.Barcode == barcode);
    }
}
