using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.Get;
using IndyPOS.Domain.Entities.Core;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Products.Queries;

public class GetProductsQueryHandlerTests
{
    private static List<Product> CreateTestProducts(int count = 3)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Barcode = $"BARCODE{i}",
                Name = $"Product {i}",
                Description = $"Description {i}",
                Category = $"Category {i}",
                Brand = $"Brand {i}",
                Manufacturer = $"Manufacturer {i}",
                UnitPrice = 10.00m * i,
                IsActive = true
            })
            .ToList();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithActiveOnly_ShouldReturnActiveProducts(
        [Frozen] Mock<IProductRepository> productRepository,
        GetProductsQueryHandler sut)
    {
        // Arrange
        var products = CreateTestProducts();
        productRepository.Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(products);

        var query = new GetProductsQuery(ActiveOnly: true);

        // Act
        var result = await sut.HandleAsync(query);

        // Assert
        result.Should().HaveCount(products.Count);
        productRepository.Verify(x => x.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithActiveOnlyFalse_ShouldReturnAllProducts(
        [Frozen] Mock<IProductRepository> productRepository,
        GetProductsQueryHandler sut)
    {
        // Arrange
        var products = CreateTestProducts();
        productRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(products);

        var query = new GetProductsQuery(ActiveOnly: false);

        // Act
        var result = await sut.HandleAsync(query);

        // Assert
        result.Should().HaveCount(products.Count);
        productRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithSearchTerm_ShouldSearchProducts(
        [Frozen] Mock<IProductRepository> productRepository,
        GetProductsQueryHandler sut,
        string searchTerm)
    {
        // Arrange
        var products = CreateTestProducts();
        productRepository.Setup(x => x.SearchAsync(searchTerm, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(products);

        var query = new GetProductsQuery(ActiveOnly: true, SearchTerm: searchTerm);

        // Act
        var result = await sut.HandleAsync(query);

        // Assert
        result.Should().HaveCount(products.Count);
        productRepository.Verify(x => x.SearchAsync(searchTerm, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithCategory_ShouldFilterByCategory(
        [Frozen] Mock<IProductRepository> productRepository,
        GetProductsQueryHandler sut,
        string category)
    {
        // Arrange
        var products = CreateTestProducts();
        productRepository.Setup(x => x.GetByCategoryAsync(category, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(products);

        var query = new GetProductsQuery(ActiveOnly: true, Category: category);

        // Act
        var result = await sut.HandleAsync(query);

        // Assert
        result.Should().HaveCount(products.Count);
        productRepository.Verify(x => x.GetByCategoryAsync(category, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_SearchTermTakesPriorityOverCategory(
        [Frozen] Mock<IProductRepository> productRepository,
        GetProductsQueryHandler sut,
        string searchTerm,
        string category)
    {
        // Arrange
        var products = CreateTestProducts();
        productRepository.Setup(x => x.SearchAsync(searchTerm, true, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(products);

        var query = new GetProductsQuery(ActiveOnly: true, Category: category, SearchTerm: searchTerm);

        // Act
        var result = await sut.HandleAsync(query);

        // Assert
        productRepository.Verify(x => x.SearchAsync(searchTerm, true, It.IsAny<CancellationToken>()), Times.Once);
        productRepository.Verify(x => x.GetByCategoryAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_ShouldMapProductsToDto(
        [Frozen] Mock<IProductRepository> productRepository,
        GetProductsQueryHandler sut)
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Barcode = "12345",
            Name = "Test Product",
            Description = "Test Description",
            Category = "Test Category",
            Brand = "Test Brand",
            Manufacturer = "Test Manufacturer",
            UnitPrice = 100.50m,
            GroupPrice = 90.00m,
            GroupPriceQuantity = 10,
            IsActive = true
        };

        productRepository.Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Product> { product });

        var query = new GetProductsQuery(ActiveOnly: true);

        // Act
        var result = await sut.HandleAsync(query);

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Id.Should().Be(product.Id);
        dto.Barcode.Should().Be(product.Barcode);
        dto.Name.Should().Be(product.Name);
        dto.Description.Should().Be(product.Description);
        dto.Category.Should().Be(product.Category);
        dto.Brand.Should().Be(product.Brand);
        dto.Manufacturer.Should().Be(product.Manufacturer);
        dto.UnitPrice.Should().Be(product.UnitPrice);
        dto.GroupPrice.Should().Be(product.GroupPrice);
        dto.GroupPriceQuantity.Should().Be(product.GroupPriceQuantity);
        dto.IsActive.Should().Be(product.IsActive);
    }
}
