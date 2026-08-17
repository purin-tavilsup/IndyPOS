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
    private static Product CreateTestProduct(Guid? id = null, bool isTrackable = true)
    {
        return new Product
        {
            Id = id ?? Guid.NewGuid(),
            Barcode = "TEST123",
            Name = "Test Product",
            UnitPrice = 100m,
            IsActive = true,
            IsTrackable = isTrackable
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

        // Two ordinary, stock-tracked products produce two movements. The trip-wire that used to live
        // here is discharged: Core.Product now carries IsTrackable and
        // HandleAsync_WithANonTrackableProduct_ShouldNotCreateAnInventoryMovement below is the
        // assertion defect 7b was waiting for.
        capturedMovements.Should().HaveCount(2);
        capturedMovements![0].QuantityDelta.Should().Be(-3); // Negative for sale
        capturedMovements![1].QuantityDelta.Should().Be(-1);
        capturedMovements.All(m => m.Reason == "Sale").Should().BeTrue();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithANonTrackableProduct_ShouldNotCreateAnInventoryMovement(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        // DEFECT 7b. A non-trackable product holds no stock, so selling it must not move any.
        // Before this, the handler built a movement for EVERY line, which drove such a product's
        // stock permanently negative from its first v4 sale.
        //
        // Measured on the real stores: 21 + 7 + 1 = 29 non-trackable products, and they are exactly
        // the sold-by-hand items -- น้ำแข็ง (ice), ขนม 5 บาท, สินค้าเบ็ดเตล็ด, ตะปู.
        //
        // The flag cannot be derived from the category: every category holding a non-trackable
        // product also holds trackable ones (เบ็ดเตล็ด has 12 against 3,391), which is why this is a
        // per-product flag rather than ProductCategoryKind.Service.
        var trackableId = Guid.NewGuid();
        var untrackedId = Guid.NewGuid();

        productRepository.Setup(x => x.GetByIdAsync(trackableId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(trackableId));
        productRepository.Setup(x => x.GetByIdAsync(untrackedId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(CreateTestProduct(untrackedId, isTrackable: false));

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(OfferableWith("Cash"));

        IReadOnlyList<InventoryMovement>? capturedMovements = null;
        IReadOnlyList<InvoiceLine>? capturedLines = null;
        saleRepository.Setup(x => x.CompleteSaleAsync(
                It.IsAny<Invoice>(),
                It.IsAny<IReadOnlyList<InvoiceLine>>(),
                It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(),
                It.IsAny<OutboxEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback((Invoice _, IReadOnlyList<InvoiceLine> invoiceLines, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> movements, OutboxEvent _, CancellationToken _) =>
            {
                capturedLines = invoiceLines;
                capturedMovements = movements;
            })
            .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);

        var command = new CompleteSaleCommand(
            StoreId: "STORE-001",
            UserId: Guid.NewGuid(),
            Lines: new List<SaleLineRequest>
            {
                new(ProductId: trackableId, Quantity: 2, UnitPrice: 50m),
                new(ProductId: untrackedId, Quantity: 4, UnitPrice: 10m)
            },
            Payments: new List<SalePaymentRequest>
            {
                new(Method: "Cash", Amount: 140m)
            });

        await sut.HandleAsync(command);

        capturedLines.Should().HaveCount(2, "BOTH lines are sold and both must appear on the invoice");

        capturedMovements.Should().ContainSingle(
            "only the stock-tracked product moves stock")
            .Which.ProductId.Should().Be(trackableId);
        capturedMovements![0].QuantityDelta.Should().Be(-2);
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
