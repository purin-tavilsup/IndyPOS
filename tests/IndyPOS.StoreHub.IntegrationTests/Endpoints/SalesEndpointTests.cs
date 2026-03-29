using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.Sales;
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
                new SalePaymentRequest("Card", Amount: 100m)
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
}
