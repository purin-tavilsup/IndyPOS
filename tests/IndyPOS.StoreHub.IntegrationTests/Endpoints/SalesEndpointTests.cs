using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /sales/* endpoints.
/// </summary>
[Collection("Integration")]
public class SalesEndpointTests : IntegrationTestBase
{
    public SalesEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CompleteSale_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/sales/complete", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // The route took UserId from the body, so any caller could ring a sale up as anyone. The token is
    // the only authority; the body's UserId is ignored.
    [Fact]
    public async Task CompleteSale_WithAnotherUsersIdInTheBody_RecordsTheSaleUnderTheTokensUser()
    {
        var cashier = $"cashier_{Guid.NewGuid():N}";
        await AuthenticateAsAsync(cashier, "Password123!", UserRole.Cashier);
        var someoneElse = await CreateTestUserAsync($"other_{Guid.NewGuid():N}", "Password123!");
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        var request = new CompleteSaleRequest(
            UserId: someoneElse.Id,
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 100m)]);

        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        response.EnsureSuccessStatusCode();
        var sale = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        (await RecordedUserOfAsync(sale!.InvoiceId)).Should()
                                                    .Be(await UserIdOfAsync(cashier));
    }

    // Reads the real inventory_movement row, so it proves the save itself writes created_by_user_id.
    [Fact]
    public async Task CompleteSale_WithATrackableLine_StoresTheTokensUserOnTheMovement()
    {
        var cashier = $"cashier_{Guid.NewGuid():N}";
        await AuthenticateAsAsync(cashier, "Password123!", UserRole.Cashier);
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        var request = new CompleteSaleRequest(
            UserId: Guid.NewGuid(),
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 100m)]);

        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        response.EnsureSuccessStatusCode();
        var sale = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var movement = await db.InventoryMovements.SingleAsync(m => m.ReferenceId == sale!.InvoiceId
                                                                    && m.ProductId == product.Id);
        movement.CreatedByUserId.Should()
                                .Be(await UserIdOfAsync(cashier));
    }

    [Fact]
    public async Task CompleteSale_WithATokenWithoutAUserId_ReturnsUnauthorized()
    {
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutUserId());
        var request = new CompleteSaleRequest(
            UserId: Guid.NewGuid(),
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 100m)]);

        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> RecordedUserOfAsync(Guid invoiceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.Invoices.Where(i => i.Id == invoiceId).Select(i => i.UserId).SingleAsync();
    }

    private async Task<Guid> UserIdOfAsync(string username)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return await db.StoreUsers.Where(u => u.Username == username).Select(u => u.Id).SingleAsync();
    }

    [Fact]
    public async Task CompleteSale_WithValidData_ReturnsInvoice()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 50);
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        var request = new CompleteSaleRequest(
            UserId: user.Id,
            Lines: [new SaleLineRequest(product.Id, Quantity: 2, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 200m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.InvoiceId.Should().NotBeEmpty();
        result.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public async Task CompleteSale_WithMultiplePaymentMethods_Succeeds()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(unitPrice: 150m, initialStock: 100);
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        var request = new CompleteSaleRequest(
            UserId: user.Id,
            Lines: [new SaleLineRequest(product.Id, Quantity: 2, UnitPrice: 150m)],
            Payments:
            [
                new SalePaymentRequest("Cash", Amount: 200m),
                new SalePaymentRequest("MoneyTransfer", Amount: 100m)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalAmount.Should().Be(300m);
    }

    [Fact]
    public async Task CompleteSale_WithMultipleProducts_Succeeds()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var product1 = await CreateTestProductAsync(name: "Product1", unitPrice: 50m, initialStock: 100);
        var product2 = await CreateTestProductAsync(name: "Product2", unitPrice: 75m, initialStock: 100);
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        var request = new CompleteSaleRequest(
            UserId: user.Id,
            Lines:
            [
                new SaleLineRequest(product1.Id, Quantity: 3, UnitPrice: 50m),  // 150
                new SaleLineRequest(product2.Id, Quantity: 2, UnitPrice: 75m)   // 150
            ],
            Payments: [new SalePaymentRequest("Cash", Amount: 300m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalAmount.Should().Be(300m);
    }

    [Fact]
    public async Task CompleteSale_DeductsInventory()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 50);
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");
        var initialStock = await GetProductStockAsync(product.Id);

        var request = new CompleteSaleRequest(
            UserId: user.Id,
            Lines: [new SaleLineRequest(product.Id, Quantity: 5, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 500m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify inventory was deducted
        var newStock = await GetProductStockAsync(product.Id);
        newStock.Should().Be(initialStock - 5);
    }

    [Fact]
    public async Task CompleteSale_WithEmptyLines_ReturnsZeroTotal()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        var request = new CompleteSaleRequest(
            UserId: user.Id,
            Lines: [],
            Payments: [new SalePaymentRequest("Cash", Amount: 0m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        // Assert
        // API currently allows empty sales with zero total
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CompleteSale_AsManager_Succeeds()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(unitPrice: 200m, initialStock: 20);
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");

        var request = new CompleteSaleRequest(
            UserId: user.Id,
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 200m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 200m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales/complete", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // The body's UserId is ignored (the token decides), so any value will do.
    private async Task<CompleteSaleRequest> OneCashSaleRequestAsync()
    {
        var product = await CreateTestProductAsync(unitPrice: 10m, initialStock: 100);
        return new CompleteSaleRequest(
            UserId: Guid.NewGuid(),
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 10m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 10m)]);
    }

    private async Task<CompleteSaleResponse> CompleteAsync(CompleteSaleRequest request)
    {
        var response = await Client.PostAsJsonAsync("/sales/complete", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task CompleteSale_WithTwoConcurrentSales_AssignsDistinctNumbers()
    {
        await AuthenticateAsCashierAsync();
        var request = await OneCashSaleRequestAsync();

        var results = await Task.WhenAll(CompleteAsync(request), CompleteAsync(request));

        results.Select(r => r.InvoiceNumber).Should()
                                            .OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CompleteSale_WithValidData_ReturnsAPositiveInvoiceNumber()
    {
        await AuthenticateAsCashierAsync();

        var result = await CompleteAsync(await OneCashSaleRequestAsync());

        result.InvoiceNumber.Should()
                            .BePositive();
    }

    [Fact]
    public async Task CompleteSale_WithValidData_StoresTheReturnedNumberOnTheInvoice()
    {
        await AuthenticateAsCashierAsync();

        var result = await CompleteAsync(await OneCashSaleRequestAsync());

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        (await db.Invoices.SingleAsync(i => i.Id == result.InvoiceId)).InvoiceNumber.Should()
                                                                              .Be(result.InvoiceNumber);
    }

    [Fact]
    public async Task CompleteSale_WithValidData_CarriesTheNumberInTheOutboxEvent()
    {
        await AuthenticateAsCashierAsync();

        var result = await CompleteAsync(await OneCashSaleRequestAsync());

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var outbox = await db.OutboxEvents.SingleAsync(e =>
            e.Type == "InvoiceCompleted" && e.PayloadJson.Contains(result.InvoiceId.ToString()));
        JsonSerializer.Deserialize<InvoiceCompletedEvent>(outbox.PayloadJson)!
                      .InvoiceNumber.Should()
                                    .Be(result.InvoiceNumber);
    }
}
