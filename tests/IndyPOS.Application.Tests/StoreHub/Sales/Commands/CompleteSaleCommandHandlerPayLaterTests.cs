using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Domain.Entities.Core;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

/// <summary>
/// A v4 credit sale created no pay_later row, so the debt was invisible, could not be repaid, and
/// made the cash drawer come out short (spec 2026-09-30 §1).
/// </summary>
public class CompleteSaleCommandHandlerPayLaterTests
{
    private const decimal Price = 350m;
    private const string CustomerName = "ลุงสมชาย";

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithARefusedSale_SavesNothing(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        Arrange(saleRepository, productRepository, catalog, out var productId);

        var act = () => sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, Note: null)));

        await act.Should()
                 .ThrowAsync<SaleValidationException>();
        saleRepository.Verify(r => r.CompleteSaleAsync(
                It.IsAny<Invoice>(), It.IsAny<IReadOnlyList<InvoiceLine>>(), It.IsAny<IReadOnlyList<Payment>>(),
                It.IsAny<IReadOnlyList<InventoryMovement>>(), It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithACashSale_AttachesNoDebt(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.Cash, Price)));

        saved.Single().PayLater.Should()
                               .BeNull();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithAPayLaterPayment_AttachesItsDebt(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, CustomerName)));

        var payment = saved.Single();
        payment.PayLater.Should()
                        .BeEquivalentTo(new
                        {
                            PaymentId = payment.Id,
                            payment.InvoiceId,
                            Description = CustomerName,
                            PayLaterAmount = Price,
                            PaidAmount = 0m,
                            IsCompleted = false
                        });
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithALowerCasePayLaterCode_AttachesItsDebt(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest("paylater", Price, CustomerName)));

        saved.Single().PayLater.Should()
                               .NotBeNull();
    }

    [Theory]
    [CustomAutoData]
    public async Task HandleAsync_WithAPaddedCustomerName_StoresItTrimmed(
        [Frozen] Mock<ISaleRepository> saleRepository,
        [Frozen] Mock<IProductRepository> productRepository,
        [Frozen] Mock<IPaymentMethodCatalogService> catalog,
        CompleteSaleCommandHandler sut)
    {
        var saved = Arrange(saleRepository, productRepository, catalog, out var productId);

        await sut.HandleAsync(SaleOf(productId, new SalePaymentRequest(PaymentMethodCodes.PayLater, Price, $"  {CustomerName}  ")));

        saved.Single().PayLater!.Description.Should()
                                            .Be(CustomerName);
    }

    /// <summary>One product at <see cref="Price"/>; Cash and PayLater offerable; captures the saved payments.</summary>
    private static List<Payment> Arrange(
        Mock<ISaleRepository> saleRepository,
        Mock<IProductRepository> productRepository,
        Mock<IPaymentMethodCatalogService> catalog,
        out Guid productId)
    {
        var id = Guid.NewGuid();
        productId = id;
        var saved = new List<Payment>();

        productRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new Product { Id = id, Barcode = "P1", Name = "Nail", UnitPrice = Price, IsActive = true, IsTrackable = true });

        catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([
                   new PaymentMethod { Code = PaymentMethodCodes.Cash, DisplayName = "Cash", IsEnabled = true, StoreId = "STORE-001" },
                   new PaymentMethod { Code = PaymentMethodCodes.PayLater, DisplayName = "PayLater", IsEnabled = true, StoreId = "STORE-001" }
               ]);

        saleRepository.Setup(r => r.CompleteSaleAsync(
                          It.IsAny<Invoice>(), It.IsAny<IReadOnlyList<InvoiceLine>>(), It.IsAny<IReadOnlyList<Payment>>(),
                          It.IsAny<IReadOnlyList<InventoryMovement>>(), It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
                      .Callback((Invoice _, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> payments,
                          IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => saved.AddRange(payments))
                      .ReturnsAsync((Invoice inv, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                          IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => inv);

        return saved;
    }

    private static CompleteSaleCommand SaleOf(Guid productId, SalePaymentRequest payment) =>
        new(StoreId: "STORE-001",
            UserId: Guid.NewGuid(),
            Lines: [new SaleLineRequest(productId, Quantity: 1, UnitPrice: Price)],
            Payments: [payment]);
}
