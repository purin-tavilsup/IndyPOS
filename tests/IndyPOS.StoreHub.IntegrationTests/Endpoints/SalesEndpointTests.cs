using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
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
        var response = await Client.PostAsJsonAsync("/sales", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSale_WithAValidSale_ReturnsCreatedWithItsLocation()
    {
        await AuthenticateAsCashierAsync();
        var request = await OneCashLineAsync();

        var response = await Client.PostAsJsonAsync("/sales", request);

        var sale = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.Should()
                                                 .Be($"/sales/{sale!.InvoiceId}");
    }

    // The Location must name a real resource, readable by the same cashier who rang the sale up.
    [Fact]
    public async Task CreateSale_WithAValidSale_LocationResolvesToTheSale()
    {
        await AuthenticateAsCashierAsync();
        var created = await Client.PostAsJsonAsync("/sales", await OneCashLineAsync());
        var sale = await created.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);

        var detail = await Client.GetFromJsonAsync<InvoiceDetailDto>(created.Headers.Location, JsonOptions);

        detail!.Id.Should()
                  .Be(sale!.InvoiceId);
    }

    // The route once took UserId from the body, so any caller could ring a sale up as anyone. The token is
    // the only authority: a till that still sends a userId is ignored.
    [Fact]
    public async Task CompleteSale_WithAnotherUsersIdInTheBody_RecordsTheSaleUnderTheTokensUser()
    {
        var cashier = $"cashier_{Guid.NewGuid():N}";
        await AuthenticateAsAsync(cashier, "Password123!", UserRole.Cashier);
        var someoneElse = await CreateTestUserAsync($"other_{Guid.NewGuid():N}", "Password123!");
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        var request = new
        {
            userId = someoneElse.Id,
            lines = new[] { new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m) },
            payments = new[] { new SalePaymentRequest("Cash", Amount: 100m) }
        };

        var response = await Client.PostAsJsonAsync("/sales", request);

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
        var request = new
        {
            userId = Guid.NewGuid(),
            lines = new[] { new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m) },
            payments = new[] { new SalePaymentRequest("Cash", Amount: 100m) }
        };

        var response = await Client.PostAsJsonAsync("/sales", request);

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
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 100m)]);

        var response = await Client.PostAsJsonAsync("/sales", request);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    // A till older than this release still sends userId. The request no longer has the member, and
    // System.Text.Json ignores an unknown one by default, so the sale must still go through.
    [Fact]
    public async Task CompleteSale_WithAStaleUserIdInTheBody_AcceptsTheSale()
    {
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        var body = new
        {
            userId = Guid.NewGuid(),
            lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = 100m } },
            payments = new[] { new { method = "Cash", amount = 100m } }
        };

        var response = await Client.PostAsJsonAsync("/sales", body);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    private async Task<CompleteSaleRequest> OneCashLineAsync()
    {
        var product = await CreateTestProductAsync(unitPrice: 100m, initialStock: 10);
        return new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 100m)]);
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

        var request = new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 2, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 200m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

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

        var request = new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 2, UnitPrice: 150m)],
            Payments:
            [
                new SalePaymentRequest("Cash", Amount: 200m),
                new SalePaymentRequest("MoneyTransfer", Amount: 100m)
            ]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

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

        var request = new CompleteSaleRequest(
            Lines:
            [
                new SaleLineRequest(product1.Id, Quantity: 3, UnitPrice: 50m),  // 150
                new SaleLineRequest(product2.Id, Quantity: 2, UnitPrice: 75m)   // 150
            ],
            Payments: [new SalePaymentRequest("Cash", Amount: 300m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

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
        var initialStock = await GetProductStockAsync(product.Id);

        var request = new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 5, UnitPrice: 100m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 500m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify inventory was deducted
        var newStock = await GetProductStockAsync(product.Id);
        newStock.Should().Be(initialStock - 5);
    }

    [Fact]
    public async Task CompleteSale_WithEmptyLines_ReturnsZeroTotal()
    {
        // Arrange
        await AuthenticateAsCashierAsync();

        var request = new CompleteSaleRequest(
            Lines: [],
            Payments: [new SalePaymentRequest("Cash", Amount: 0m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales", request);

        // Assert
        // API currently allows empty sales with zero total
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalAmount.Should().Be(0m);
    }

    // System.Text.Json leaves a list the body omits as null, and the handler dereferenced it: a 500
    // instead of the Thai 400 every other refused sale gets. Only null is refused; an empty list is a
    // zero sale, pinned above.
    private static readonly object CashPayment = new { method = "Cash", amount = 0m };

    [Fact]
    public async Task CompleteSale_WithoutLines_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales", new { payments = new[] { CashPayment } });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteSale_WithoutLines_ReturnsTheThaiReason()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales", new { payments = new[] { CashPayment } });

        (await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions))!.Error.Should()
                                                                          .Be("ข้อมูลรายการสินค้าไม่ถูกต้อง");
    }

    [Fact]
    public async Task CompleteSale_WithANullLine_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales",
                                                    new { lines = new object?[] { null }, payments = new[] { CashPayment } });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteSale_WithoutPayments_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales", new { lines = Array.Empty<object>() });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteSale_WithANullPayment_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/sales",
                                                    new { lines = Array.Empty<object>(), payments = new object?[] { null } });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteSale_AsManager_Succeeds()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(unitPrice: 200m, initialStock: 20);

        var request = new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 200m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 200m)]);

        // Act
        var response = await Client.PostAsJsonAsync("/sales", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // The token decides the seller; the body carries only lines and payments.
    private async Task<CompleteSaleRequest> OneCashSaleRequestAsync()
    {
        var product = await CreateTestProductAsync(unitPrice: 10m, initialStock: 100);
        return new CompleteSaleRequest(
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 10m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 10m)]);
    }

    private async Task<CompleteSaleResponse> CompleteAsync(CompleteSaleRequest request)
    {
        var response = await Client.PostAsJsonAsync("/sales", request);
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

    private sealed record ErrorBody(string Error);
}
