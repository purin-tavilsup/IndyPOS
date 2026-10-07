using System.Net.Http.Json;
using FluentAssertions;
using SalesSummary = IndyPOS.Application.Common.Models.SalesSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.ValueObjects;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

/// <summary>
/// The sales summary's per-store lists. The hosts' databases are shared across this collection, so
/// each test measures a before/after difference or a code list, never an absolute total.
/// </summary>
[Collection(StoreProfilesCollection.Name)]
public class SalesSummaryPerStoreTests(StoreProfileHosts hosts)
{
    private static readonly string Today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");

    [Fact]
    public async Task SalesSummary_ForMimyMart_HasNoServiceSales()
    {
        var summary = await SummaryAsync("MimyMart");

        summary.ServiceSales.Should()
                            .BeEmpty();
    }

    [Fact]
    public async Task SalesSummary_ForMimyMart_ListsOnlyItsEnabledMethods()
    {
        var summary = await SummaryAsync("MimyMart");

        summary.PaymentsByMethod.Select(p => p.Code).Should()
                                                    .Equal("Cash", "MoneyTransfer");
    }

    [Fact]
    public async Task SalesSummary_ForGeneralHardwareWithAWeWinSaleToday_ListsWeWin()
    {
        await SeedPaymentAsync("GeneralHardware", "WeWin", 40m);

        var summary = await SummaryAsync("GeneralHardware");

        summary.PaymentsByMethod.Should()
                                .Contain(p => p.Code == "WeWin" && p.DisplayName == "เราชนะ");
    }

    [Fact]
    public async Task SalesSummary_ForMimyShopAfterADeliverySale_AddsItToDelivery()
    {
        var before = await SummaryAsync("MimyShop");

        await SellAsync("MimyShop", ServiceProductBarcodes.Delivery, 30m);
        var after = await SummaryAsync("MimyShop");

        Total(after, ServiceProductBarcodes.Delivery).Should()
                                                     .Be(Total(before, ServiceProductBarcodes.Delivery) + 30m);
    }

    [Fact]
    public async Task SalesSummary_ForMimyShop_ListsBothServicesInOrder()
    {
        var summary = await SummaryAsync("MimyShop");

        summary.ServiceSales.Select(s => s.Barcode).Should()
                                                   .Equal(ServiceProductBarcodes.All);
    }

    // The overview's "ยอดขาย : ทั้งหมด" tile reads LinesTotal where the legacy summary is not fetched, so the
    // two must give the same figure.
    [Fact]
    public async Task SalesSummary_AfterASale_HasTheLegacySummarysInvoiceTotal()
    {
        await SellAsync("MimyShop", ServiceProductBarcodes.Documents, 15m);
        var client = await hosts.SignedInAsync("MimyShop", "manager", "manager123");

        var summary = await SummaryAsync("MimyShop");
        var legacy = await client.GetFromJsonAsync<SalesSummary>($"/reports/legacy/sales-summary?fromDate={Today}&toDate={Today}");

        summary.LinesTotal.Should()
                          .Be(legacy!.InvoiceTotal);
    }

    private static decimal Total(SalesSummaryDto summary, string barcode) =>
        summary.ServiceSales.Single(s => s.Barcode == barcode).Total;

    private async Task<SalesSummaryDto> SummaryAsync(string key)
    {
        var client = await hosts.SignedInAsync(key, "manager", "manager123");
        return (await client.GetFromJsonAsync<SalesSummaryDto>(
            $"/reports/sales-summary?fromDate={Today}&toDate={Today}&topProductsCount=0"))!;
    }

    private async Task SellAsync(string key, string barcode, decimal price)
    {
        var client = await hosts.SignedInAsync(key);
        var product = await ProductAsync(key, barcode);
        var response = await client.PostAsJsonAsync("/sales", new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: price)],
            Payments: [new SalePaymentRequest("Cash", price)]));
        response.EnsureSuccessStatusCode();
    }

    private async Task SeedPaymentAsync(string key, string method, decimal amount)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), StoreId = Profiles.Find(key)!.StoreId, UserId = Guid.NewGuid(),
            TotalAmount = amount, CreatedUtc = now, LastModifiedUtc = now
        };
        invoice.Payments.Add(new Payment { Id = Guid.NewGuid(), InvoiceId = invoice.Id, Method = method, Amount = amount, CreatedUtc = now });
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
    }

    private async Task<Product> ProductAsync(string key, string barcode)
    {
        await using var scope = hosts.ServicesFor(key).CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Products
                          .AsNoTracking()
                          .SingleAsync(p => p.Barcode == barcode);
    }
}
