using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Spec 2026-09-30: a v4 credit sale creates its debt, like a migrated v3 one. The shared database
/// is never reset, so tests find their own rows by invoice id, and compare totals before and after.
/// </summary>
[Collection("Integration")]
public class PayLaterSaleEndpointTests : IntegrationTestBase
{
    private const decimal Price = 350m;
    private const string CustomerName = "ลุงสมชาย";

    public PayLaterSaleEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private sealed record ErrorBody(string Error);

    [Fact]
    public async Task CompleteSale_WithAPayLaterPaymentWithoutANote_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales", await CreditSaleAsync(note: null));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteSale_WithAPayLaterPaymentWithoutANote_ReturnsTheThaiReason()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales", await CreditSaleAsync(note: null));

        (await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions))!.Error.Should()
                                                                          .Be("กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี");
    }

    [Fact]
    public async Task CompleteSale_WithAPayLaterPaymentWithoutANote_SavesNoInvoice()
    {
        await AuthenticateAsCashierAsync();
        var request = await CreditSaleAsync(note: null);
        var before = await CountInvoicesAsync();

        await Client.PostAsJsonAsync("/sales", request);

        (await CountInvoicesAsync()).Should()
                                    .Be(before);
    }

    [Fact]
    public async Task CompleteSale_WithPayLaterMixedWithCash_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();
        var request = await CreditSaleAsync(CustomerName, new SalePaymentRequest(PaymentMethodCodes.Cash, 1m));

        var response = await Client.PostAsJsonAsync("/sales", request);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteSale_WithPayLaterMixedWithCash_SavesNoInvoice()
    {
        await AuthenticateAsCashierAsync();
        var request = await CreditSaleAsync(CustomerName, new SalePaymentRequest(PaymentMethodCodes.Cash, 1m));
        var before = await CountInvoicesAsync();

        await Client.PostAsJsonAsync("/sales", request);

        (await CountInvoicesAsync()).Should()
                                    .Be(before);
    }

    // Was an unmapped InvalidOperationException: a 500 in production (TestServer rethrows it instead).
    [Fact]
    public async Task CompleteSale_WithAMethodNotOfferable_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();
        var request = await CreditSaleAsync(CustomerName) with { Payments = [new SalePaymentRequest(PaymentMethodCodes.M33WeLove, Price)] };

        var response = await Client.PostAsJsonAsync("/sales", request);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    // The regression test: on development this finds no row.
    [Fact]
    public async Task CompleteSale_WithAPayLaterPayment_CreatesItsDebt()
    {
        await AuthenticateAsCashierAsync();

        var sale = await SellOnCreditAsync();

        (await CountDebtsOfAsync(sale.InvoiceId)).Should()
                                                 .Be(1);
    }

    [Fact]
    public async Task GetPayLater_AfterAV4CreditSale_ListsItsDebt()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOnCreditAsync();

        var list = await Client.GetFromJsonAsync<GetPayLaterResponse>("/pay-later", JsonOptions);

        list!.Items.Should()
                   .ContainSingle(d => d.InvoiceId == sale.InvoiceId && d.Description == CustomerName);
    }

    [Fact]
    public async Task RecordPayment_OnAV4CreditSale_ReducesWhatIsOwed()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOnCreditAsync();
        var debt = (await Client.GetFromJsonAsync<GetPayLaterResponse>("/pay-later", JsonOptions))!
                   .Items.Single(d => d.InvoiceId == sale.InvoiceId);

        var response = await Client.PostAsJsonAsync($"/pay-later/{debt.Id}/payments", new RecordPaymentRequest(100m));

        (await response.Content.ReadFromJsonAsync<PayLaterDto>(JsonOptions))!.RemainingAmount.Should()
                                                                             .Be(Price - 100m);
    }

    // The drawer read credit sales through pay_later rows, so a v4 one counted as cash (spec §1).
    [Fact]
    public async Task CashSummary_AfterAV4CreditSale_CountsItAsCredit()
    {
        await AuthenticateAsCashierAsync();
        var before = await TodaysCreditTotalAsync();

        await SellOnCreditAsync();

        (await TodaysCreditTotalAsync()).Should()
                                        .Be(before + Price);
    }

    private async Task<decimal> TodaysCreditTotalAsync()
    {
        var summary = await Client.GetFromJsonAsync<CashDrawerSummaryDto>("/cash/summary", JsonOptions);

        return summary!.PayLaterGeneralProductsTotal + summary.PayLaterHardwareProductsTotal;
    }

    private async Task<int> CountInvoicesAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.Invoices.CountAsync();
    }

    private async Task<CompleteSaleResponse> SellOnCreditAsync(string? note = CustomerName)
    {
        var response = await Client.PostAsJsonAsync("/sales", await CreditSaleAsync(note));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    private async Task<CompleteSaleRequest> CreditSaleAsync(string? note, params SalePaymentRequest[] extraPayments)
    {
        var product = await CreateTestProductAsync(unitPrice: Price, initialStock: 10);

        return new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: Price)],
            Payments: [new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, note), .. extraPayments]);
    }

    private async Task<int> CountDebtsOfAsync(Guid invoiceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.PayLaters.CountAsync(p => p.InvoiceId == invoiceId);
    }
}
