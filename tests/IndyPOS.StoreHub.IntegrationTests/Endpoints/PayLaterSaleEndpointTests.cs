using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
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

    // The regression test: on development this finds no row.
    [Fact]
    public async Task CompleteSale_WithAPayLaterPayment_CreatesItsDebt()
    {
        await AuthenticateAsCashierAsync();

        var sale = await SellOnCreditAsync();

        (await CountDebtsOfAsync(sale.InvoiceId)).Should()
                                                 .Be(1);
    }

    private async Task<CompleteSaleResponse> SellOnCreditAsync(string? note = CustomerName)
    {
        var response = await Client.PostAsJsonAsync("/sales/complete", await CreditSaleAsync(note));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    private async Task<CompleteSaleRequest> CreditSaleAsync(string? note, params SalePaymentRequest[] extraPayments)
    {
        var product = await CreateTestProductAsync(unitPrice: Price, initialStock: 10);
        var seller = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        return new CompleteSaleRequest(
            UserId: seller.Id,
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
