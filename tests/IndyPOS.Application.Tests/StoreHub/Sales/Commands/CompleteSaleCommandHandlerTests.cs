using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Domain.Entities.Core;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

public class CompleteSaleCommandHandlerTests
{
    private static Product CreateTestProduct(Guid? id = null)
    {
        return new Product
        {
            Id = id ?? Guid.NewGuid(),
            Barcode = "TEST123",
            Name = "Test Product",
            UnitPrice = 100m,
            IsActive = true
        };
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_ShouldCreateInvoiceWithCorrectTotal(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateTestProduct(productId);

        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(product);

        saleRepository.Setup(x => x.CompleteSaleAsync(
                It.IsAny<Invoice>(),
                It.IsAny<IReadOnlyList<InvoiceLine>>(),
                It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(),
                It.IsAny<OutboxEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);

        var command = new CompleteSaleCommand(
            StoreId: "STORE-001",
            UserId: 1,
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: productId, Quantity: 2, UnitPrice: 100m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "Cash", Amount: 200m)
            });

        // Act
        var result = await sut.HandleAsync(command);

        // Assert
        result.TotalAmount.Should().Be(200m); // 2 * 100
        result.InvoiceId.Should().NotBeEmpty();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_ShouldCreateInventoryMovementsForEachLine(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        productRepository.Setup(x => x.GetByIdAsync(product1Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(product1Id));
        productRepository.Setup(x => x.GetByIdAsync(product2Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(product2Id));

        IReadOnlyList<InventoryMovement>? capturedMovements = null;
        saleRepository.Setup(x => x.CompleteSaleAsync(
                It.IsAny<Invoice>(),
                It.IsAny<IReadOnlyList<InvoiceLine>>(),
                It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(),
                It.IsAny<OutboxEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback((Invoice _, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> movements, OutboxEvent _, CancellationToken _) =>
            {
                capturedMovements = movements;
            })
            .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);

        var command = new CompleteSaleCommand(
            StoreId: "STORE-001",
            UserId: 1,
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: product1Id, Quantity: 3, UnitPrice: 50m),
                new(ProductId: product2Id, Quantity: 1, UnitPrice: 100m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "Cash", Amount: 250m)
            });

        // Act
        await sut.HandleAsync(command);

        // Assert
        capturedMovements.Should().NotBeNull();
        capturedMovements.Should().HaveCount(2);
        capturedMovements![0].QuantityDelta.Should().Be(-3); // Negative for sale
        capturedMovements![1].QuantityDelta.Should().Be(-1);
        capturedMovements.All(m => m.Reason == "Sale").Should().BeTrue();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_ShouldCreateOutboxEvent(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var productId = Guid.NewGuid();
        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(productId));

        OutboxEvent? capturedOutbox = null;
        saleRepository.Setup(x => x.CompleteSaleAsync(
                It.IsAny<Invoice>(),
                It.IsAny<IReadOnlyList<InvoiceLine>>(),
                It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(),
                It.IsAny<OutboxEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback((Invoice _, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent outbox, CancellationToken _) =>
            {
                capturedOutbox = outbox;
            })
            .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);

        var command = new CompleteSaleCommand(
            StoreId: "STORE-001",
            UserId: 1,
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: productId, Quantity: 1, UnitPrice: 100m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "Cash", Amount: 100m)
            });

        // Act
        await sut.HandleAsync(command);

        // Assert
        capturedOutbox.Should().NotBeNull();
        capturedOutbox!.Type.Should().Be("InvoiceCompleted");
        capturedOutbox.StoreId.Should().Be("STORE-001");
        capturedOutbox.Status.Should().Be("Pending");
        capturedOutbox.PayloadJson.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_ShouldSnapshotProductName(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Special Product Name",
            Barcode = "TEST",
            UnitPrice = 100m
        };

        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(product);

        IReadOnlyList<InvoiceLine>? capturedLines = null;
        saleRepository.Setup(x => x.CompleteSaleAsync(
                It.IsAny<Invoice>(),
                It.IsAny<IReadOnlyList<InvoiceLine>>(),
                It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(),
                It.IsAny<OutboxEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback((Invoice _, IReadOnlyList<InvoiceLine> lines, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) =>
            {
                capturedLines = lines;
            })
            .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);

        var command = new CompleteSaleCommand(
            StoreId: "STORE-001",
            UserId: 1,
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: productId, Quantity: 1, UnitPrice: 100m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "Cash", Amount: 100m)
            });

        // Act
        await sut.HandleAsync(command);

        // Assert
        capturedLines.Should().NotBeNull();
        capturedLines![0].ProductName.Should().Be("Special Product Name");
    }
}
