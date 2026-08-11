using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /products/* endpoints.
/// </summary>
[Collection("Integration")]
public class ProductsEndpointTests : IntegrationTestBase
{
    public ProductsEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProducts_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_AsAuthenticatedUser_ReturnsProducts()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync();

        // Act
        var response = await Client.GetAsync("/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        products.Should().NotBeNull();
        products!.Should().Contain(p => p.Id == product.Id);
    }

    [Fact]
    public async Task GetProducts_WithActiveOnly_ReturnsOnlyActiveProducts()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var activeProduct = await CreateTestProductAsync(name: "Active Product", isActive: true);
        await CreateTestProductAsync(name: "Inactive Product", isActive: false);

        // Act
        var response = await Client.GetAsync("/products?activeOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        products.Should().NotBeNull();
        products!.Should().OnlyContain(p => p.IsActive);
        products.Should().Contain(p => p.Id == activeProduct.Id);
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_ReturnsMatchingProducts()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var searchableProduct = await CreateTestProductAsync(name: "UniqueSearchableName123");
        await CreateTestProductAsync(name: "OtherProduct");

        // Act
        var response = await Client.GetAsync("/products?search=UniqueSearchable");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        products.Should().NotBeNull();
        products!.Should().Contain(p => p.Id == searchableProduct.Id);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedProduct()
    {
        // Arrange
        await AuthenticateAsManagerAsync(); // Manager can create products

        var command = new CreateProductCommand
        {
            Barcode = $"BAR{Guid.NewGuid():N}"[..13],
            Name = "Test Product",
            Description = "A test product",
            Category = ProductCategoryCodes.Miscellaneous,
            UnitPrice = 99.99m,
            InitialQuantity = 50
        };

        // Act
        var response = await Client.PostAsJsonAsync("/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        product.Should().NotBeNull();
        product!.Barcode.Should().Be(command.Barcode);
        product.Name.Should().Be(command.Name);
        product.UnitPrice.Should().Be(command.UnitPrice);
    }

    [Fact]
    public async Task CreateProduct_AsCashier_ReturnsForbidden()
    {
        // Arrange
        await AuthenticateAsCashierAsync(); // Cashier cannot create products

        var command = new CreateProductCommand
        {
            Barcode = $"BAR{Guid.NewGuid():N}"[..13],
            Name = "Test Product",
            Category = ProductCategoryCodes.Miscellaneous,
            UnitPrice = 99.99m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateBarcode_ReturnsConflict()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var existingProduct = await CreateTestProductAsync();

        var command = new CreateProductCommand
        {
            Barcode = existingProduct.Barcode, // Same barcode as existing
            Name = "Duplicate Product",
            Category = ProductCategoryCodes.Miscellaneous,
            UnitPrice = 50m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/products", command);

        // Assert
        // InvalidOperationException from the handler is mapped to 409 Conflict
        // (not an unhandled 500 Internal Server Error).
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Error.Should().Contain("already exists");
    }

    private record ErrorResponse(string Error);

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsUpdatedProduct()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var existingProduct = await CreateTestProductAsync();

        var command = new UpdateProductCommand
        {
            Id = existingProduct.Id,
            Barcode = existingProduct.Barcode,
            Name = "Updated Product Name",
            Category = ProductCategoryCodes.Miscellaneous,
            UnitPrice = 149.99m
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/products/{existingProduct.Id}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Updated Product Name");
        product.UnitPrice.Should().Be(149.99m);
    }

    [Fact]
    public async Task UpdateProduct_NonExistent_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var nonExistentId = Guid.NewGuid();

        var command = new UpdateProductCommand
        {
            Id = nonExistentId,
            Barcode = "NONEXIST123",
            Name = "Ghost Product",
            Category = ProductCategoryCodes.Miscellaneous,
            UnitPrice = 1m
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/products/{nonExistentId}", command);

        // Assert
        // ProductNotFoundException maps to 404; 409 stays reserved for real conflicts
        // (duplicate barcode, store-type violation).
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteProduct_AsManager_SoftDeletesProduct()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync();

        // Act
        var response = await Client.DeleteAsync($"/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify product is soft-deleted (not active)
        var getResponse = await Client.GetAsync($"/products?activeOnly=false");
        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions);
        var deletedProduct = products?.FirstOrDefault(p => p.Id == product.Id);
        deletedProduct?.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProduct_AsCashier_ReturnsForbidden()
    {
        // Arrange
        await AuthenticateAsCashierAsync();
        var product = await CreateTestProductAsync();

        // Act
        var response = await Client.DeleteAsync($"/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdjustQuantity_AsManager_ShouldApplyTheDeltaToTheBalance()
    {
        // Arrange
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(initialStock: 100);

        // Act
        var response = await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new
        {
            delta = 50,
            reason = "Restock"
        });

        // Assert
        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AdjustQuantityResponse>(JsonOptions);
        result!.Quantity.Should()
                        .Be(150);

        (await GetProductStockAsync(product.Id)).Should()
                                                .Be(150);
    }

    [Fact]
    public async Task AdjustQuantity_WithASaleInBetween_ShouldNotSwallowTheSale()
    {
        // The reason this endpoint takes a delta rather than a target quantity. The
        // operator sees 100 and restocks by 50; meanwhile the other till sells 30. The
        // answer is 120. A target-based endpoint would write 150 and lose the sale.
        // If anyone reverts to target semantics, this test must fail.
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(initialStock: 100);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
            db.InventoryMovements.Add(new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = "test-store",
                ProductId = product.Id,
                QuantityDelta = -30,
                Reason = "Sale",
                CreatedUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new
        {
            delta = 50,
            reason = "Restock"
        });

        (await GetProductStockAsync(product.Id)).Should()
                                                .Be(120);
    }

    [Fact]
    public async Task AdjustQuantity_WithAZeroDelta_ShouldReturnBadRequest()
    {
        // Rejected at the boundary rather than silently ignored, so a UI bug that sends
        // a no-op adjustment is visible instead of looking like it worked.
        await AuthenticateAsManagerAsync();
        var product = await CreateTestProductAsync(initialStock: 100);

        var response = await Client.PostAsJsonAsync($"/products/{product.Id}/adjust-quantity", new
        {
            delta = 0,
            reason = "Restock"
        });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateNextBarcode_AsManager_ReturnsNextBarcode()
    {
        // Arrange
        await AuthenticateAsManagerAsync();

        // Act
        var response = await Client.PostAsync("/products/next-barcode", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<NextBarcodeResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Barcode.Should().NotBeNullOrEmpty();
    }

    private record NextBarcodeResponse(string Barcode);
}
