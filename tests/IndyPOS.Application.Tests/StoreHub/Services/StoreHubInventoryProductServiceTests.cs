using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Domain.Events;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class StoreHubInventoryProductServiceTests
{
    private readonly Mock<IStoreHubClient> _storeHubClientMock;
    private readonly Mock<IProductCacheService> _productCacheServiceMock;
    private readonly Mock<IStoreConstants> _storeConstantsMock;
    private readonly Mock<IEventAggregator> _eventAggregatorMock;
    private readonly Mock<ILogger<StoreHubInventoryProductService>> _loggerMock;
    private readonly StoreHubInventoryProductService _sut;

    public StoreHubInventoryProductServiceTests()
    {
        _storeHubClientMock = new Mock<IStoreHubClient>();
        _productCacheServiceMock = new Mock<IProductCacheService>();
        _eventAggregatorMock = new Mock<IEventAggregator>();
        _loggerMock = new Mock<ILogger<StoreHubInventoryProductService>>();

        // Setup event aggregator
        _eventAggregatorMock.Setup(x => x.GetEvent<InventoryProductAddedEvent>())
            .Returns(new InventoryProductAddedEvent());
        _eventAggregatorMock.Setup(x => x.GetEvent<InventoryProductUpdatedEvent>())
            .Returns(new InventoryProductUpdatedEvent());
        _eventAggregatorMock.Setup(x => x.GetEvent<InventoryProductDeletedEvent>())
            .Returns(new InventoryProductDeletedEvent());

        _sut = new StoreHubInventoryProductService(
            _storeHubClientMock.Object,
            _productCacheServiceMock.Object,
            _eventAggregatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallStoreHubClient_AndUpdateCache()
    {
        // Arrange
        var request = new CreateInventoryProductRequest
        {
            Barcode = "1234567890123",
            Description = "Test Product",
            Category = ProductCategoryCodes.Beverages,
            UnitPrice = 100m,
            QuantityInStock = 10,
            IsTrackable = true
        };

        var productDto = new ProductDto(
            Id: Guid.NewGuid(),
            Barcode: request.Barcode,
            Name: request.Description,
            Description: request.Description,
            Category: "เครื่องดื่ม",
            Brand: null,
            Manufacturer: null,
            UnitPrice: request.UnitPrice,
            GroupPrice: null,
            GroupPriceQuantity: null,
            IsActive: true);

        _storeHubClientMock.Setup(x => x.CreateProductAsync(
                It.IsAny<CreateProductCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(productDto);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productDto.Id);
        result.Barcode.Should().Be(productDto.Barcode);
        result.Description.Should().Be(productDto.Name);

        _storeHubClientMock.Verify(x => x.CreateProductAsync(
            It.Is<CreateProductCommand>(c =>
                c.Barcode == request.Barcode &&
                c.Name == request.Description &&
                c.UnitPrice == request.UnitPrice),
            It.IsAny<CancellationToken>()), Times.Once);

        _productCacheServiceMock.Verify(x => x.UpsertProduct(productDto), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldCallStoreHubClient_AndUpdateCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateInventoryProductRequest
        {
            Id = productId,
            Description = "Updated Product",
            Category = ProductCategoryCodes.Food,
            UnitPrice = 150m,
            QuantityInStock = 20
        };

        var existingProduct = new ProductDto(
            Id: productId,
            Barcode: "1234567890123",
            Name: "Old Name",
            Description: "Old Description",
            Category: "เครื่องดื่ม",
            Brand: null,
            Manufacturer: null,
            UnitPrice: 100m,
            GroupPrice: null,
            GroupPriceQuantity: null,
            IsActive: true);

        var updatedProduct = new ProductDto(
            Id: productId,
            Barcode: "1234567890123",
            Name: request.Description,
            Description: request.Description,
            Category: "อาหาร",
            Brand: null,
            Manufacturer: null,
            UnitPrice: request.UnitPrice,
            GroupPrice: null,
            GroupPriceQuantity: null,
            IsActive: true);

        _productCacheServiceMock.Setup(x => x.GetById(productId)).Returns(existingProduct);
        _storeHubClientMock.Setup(x => x.UpdateProductAsync(
                It.IsAny<UpdateProductCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProduct);

        // Act
        var result = await _sut.UpdateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Description.Should().Be(request.Description);

        _storeHubClientMock.Verify(x => x.UpdateProductAsync(
            It.Is<UpdateProductCommand>(c => c.Id == productId && c.Name == request.Description),
            It.IsAny<CancellationToken>()), Times.Once);

        _productCacheServiceMock.Verify(x => x.UpsertProduct(updatedProduct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallStoreHubClient_AndRemoveFromCache()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _storeHubClientMock.Setup(x => x.DeleteProductAsync(productId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(productId);

        // Assert
        _storeHubClientMock.Verify(x => x.DeleteProductAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _productCacheServiceMock.Verify(x => x.RemoveProduct(productId), Times.Once);
    }

    [Fact]
    public async Task AdjustQuantityAsync_ShouldCallStoreHubClient_AndUpdateCache()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var targetQuantity = 50;
        var reason = "Manual adjustment";

        var adjustedProduct = new ProductDto(
            Id: productId,
            Barcode: "1234567890123",
            Name: "Test Product",
            Description: "Test Description",
            Category: "เครื่องดื่ม",
            Brand: null,
            Manufacturer: null,
            UnitPrice: 100m,
            GroupPrice: null,
            GroupPriceQuantity: null,
            IsActive: true);

        _storeHubClientMock.Setup(x => x.AdjustProductQuantityAsync(
                productId,
                It.Is<AdjustQuantityRequest>(r => r.Delta == targetQuantity && r.Reason == reason),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(adjustedProduct);

        // Act
        var result = await _sut.AdjustQuantityAsync(productId, targetQuantity, reason);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);

        _productCacheServiceMock.Verify(x => x.UpsertProduct(adjustedProduct), Times.Once);
    }

    [Fact]
    public async Task GenerateBarcodeAsync_ShouldCallStoreHubClient()
    {
        // Arrange
        var expectedBarcode = "1234567890123";
        _storeHubClientMock.Setup(x => x.GenerateBarcodeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBarcode);

        // Act
        var result = await _sut.GenerateBarcodeAsync();

        // Assert
        result.Should().Be(expectedBarcode);
        _storeHubClientMock.Verify(x => x.GenerateBarcodeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByBarcodeAsync_ShouldReturnFromCache()
    {
        // Arrange
        var barcode = "1234567890123";
        var cachedProduct = new ProductDto(
            Id: Guid.NewGuid(),
            Barcode: barcode,
            Name: "Test Product",
            Description: "Test Description",
            Category: "เครื่องดื่ม",
            Brand: "Test Brand",
            Manufacturer: "Test Manufacturer",
            UnitPrice: 100m,
            GroupPrice: 90m,
            GroupPriceQuantity: 10,
            IsActive: true);

        _productCacheServiceMock.Setup(x => x.GetByBarcode(barcode)).Returns(cachedProduct);

        // Act
        var result = await _sut.GetByBarcodeAsync(barcode);

        // Assert
        result.Should().NotBeNull();
        result.Barcode.Should().Be(barcode);
        result.Description.Should().Be(cachedProduct.Name);
        result.UnitPrice.Should().Be(cachedProduct.UnitPrice);
        result.GroupPrice.Should().Be(cachedProduct.GroupPrice ?? 0m);
        result.GroupPriceQuantity.Should().Be(cachedProduct.GroupPriceQuantity);
    }

    [Fact]
    public async Task GetByBarcodeAsync_WhenNotFound_ShouldThrow()
    {
        // Arrange
        var barcode = "nonexistent";
        _productCacheServiceMock.Setup(x => x.GetByBarcode(barcode)).Returns((ProductDto?)null);

        // Act
        var act = async () => await _sut.GetByBarcodeAsync(barcode);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{barcode}*");
    }

    [Fact]
    public async Task GetByCategoryIdAsync_ShouldReturnFilteredProducts()
    {
        // Arrange
        var categoryCode = ProductCategoryCodes.Beverages;

        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "123", "Product 1", "Desc 1", categoryCode, null, null, 10m, null, null, true),
            new(Guid.NewGuid(), "456", "Product 2", "Desc 2", categoryCode, null, null, 20m, null, null, true),
            new(Guid.NewGuid(), "789", "Product 3", "Desc 3", ProductCategoryCodes.Food, null, null, 30m, null, null, true)
        };

        _productCacheServiceMock.Setup(x => x.GetAll()).Returns(products);

        // Act
        var result = await _sut.GetByCategoryAsync(categoryCode);

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.Category == categoryCode).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCachedProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "123", "Product 1", "Desc 1", "เครื่องดื่ม", null, null, 10m, null, null, true),
            new(Guid.NewGuid(), "456", "Product 2", "Desc 2", "อาหาร", null, null, 20m, null, null, true)
        };

        _productCacheServiceMock.Setup(x => x.GetAll()).Returns(products);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.Barcode).Should().BeEquivalentTo("123", "456");
    }

    [Fact]
    public async Task SearchByDescriptionAsync_ShouldReturnMatchingProducts()
    {
        // Arrange
        var keyword = "test";
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "123", "Test Product", "Test Desc", "เครื่องดื่ม", null, null, 10m, null, null, true)
        };

        _productCacheServiceMock.Setup(x => x.Search(keyword)).Returns(products);

        // Act
        var result = await _sut.SearchByDescriptionAsync(keyword);

        // Assert
        result.Should().HaveCount(1);
        result.First().Description.Should().Contain("Test");
    }

    [Fact]
    public async Task SearchByBrandAsync_ShouldReturnMatchingProducts()
    {
        // Arrange
        var keyword = "Apple";
        var allProducts = new List<ProductDto>
        {
            new(Guid.NewGuid(), "123", "iPhone", "Phone", "Tech", "Apple", null, 1000m, null, null, true),
            new(Guid.NewGuid(), "456", "Galaxy", "Phone", "Tech", "Samsung", null, 900m, null, null, true)
        };

        _productCacheServiceMock.Setup(x => x.GetAll()).Returns(allProducts);

        // Act
        var result = await _sut.SearchByBrandAsync(keyword);

        // Assert
        result.Should().HaveCount(1);
        result.First().Brand.Should().Be("Apple");
    }
}
