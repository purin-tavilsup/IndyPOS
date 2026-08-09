using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
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

    private static List<PaymentMethod> OfferableWith(params string[] codes)
    {
        return codes.Select(code => new PaymentMethod
        {
            Code = code,
            DisplayName = code,
            IsEnabled = true,
            StoreId = "STORE-001"
        }).ToList();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_ShouldCreateInvoiceWithCorrectTotal(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateTestProduct(productId);

        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(product);

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));

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
            UserId: Guid.NewGuid(),
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
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        productRepository.Setup(x => x.GetByIdAsync(product1Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(product1Id));
        productRepository.Setup(x => x.GetByIdAsync(product2Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(product2Id));

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));

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
            UserId: Guid.NewGuid(),
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

        // DEFECT 7b -- this is NOT a pinning test. Two ordinary products SHOULD produce two
        // movements, so the assertion below is correct as it stands.
        //
        // It is a trip-wire. v4's Core.Product has no IsTrackable, so CompleteSaleCommandHandler
        // (:89-100) builds a movement for EVERY line -- including services, which have no stock.
        // 21/7/1 products across the three real stores are non-trackable, and the migration gives
        // all 29 of them stock, so the harm starts at their first v4 sale.
        //
        // The day Product gains IsTrackable, CreateTestProduct below will not set it and this count
        // will break. DO NOT repair the number. Add a non-trackable line to this test and assert it
        // produces NO movement -- that is the assertion defect 7b has been waiting for.
        //
        // Defect 7b is deliberately unpinned: "non-trackable" cannot be expressed until the flag
        // exists, so any pin today would just duplicate this test. See PLAN.md's defect table.
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
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var productId = Guid.NewGuid();
        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(productId));

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));

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
            UserId: Guid.NewGuid(),
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
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
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

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));

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
            UserId: Guid.NewGuid(),
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

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WhenPaymentMethodNotOfferable_ShouldReject(
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // "M33WeLove" is a real-but-disabled campaign code, deliberately absent from the
        // stubbed offerable set below (only "Cash" is offerable). PayLater used to be the only
        // rejected method under the old handler, which wouldn't discriminate this generalized
        // catalog-membership check.
        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));

        var command = new CompleteSaleCommand(
            StoreId: "STORE-001",
            UserId: Guid.NewGuid(),
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: productId, Quantity: 2, UnitPrice: 100m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "M33WeLove", Amount: 200m)
            });

        // Act
        var act = () => sut.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WhenAllPaymentMethodsOfferable_ShouldComplete(
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        CompleteSaleCommandHandler sut)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateTestProduct(productId);

        productRepository.Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(product);

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));

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
            UserId: Guid.NewGuid(),
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
        result.Should().NotBeNull();
    }
}
