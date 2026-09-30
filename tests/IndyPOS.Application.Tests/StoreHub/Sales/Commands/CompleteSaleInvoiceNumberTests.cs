using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

public class CompleteSaleInvoiceNumberTests
{
    private const long ReservedNumber = 139_759;
    private const string StoreId = "STORE-001";
    private const string UnofferableMethod = "Bitcoin";

    private readonly Mock<ISaleRepository> _sales = new();
    private readonly Mock<IPaymentMethodCatalogService> _catalog = new();
    private Invoice? _savedInvoice;
    private OutboxEvent? _savedEvent;

    public CompleteSaleInvoiceNumberTests()
    {
        _sales.Setup(s => s.ReserveInvoiceNumberAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(ReservedNumber);

        _sales.Setup(s => s.CompleteSaleAsync(
                  It.IsAny<Invoice>(),
                  It.IsAny<IReadOnlyList<InvoiceLine>>(),
                  It.IsAny<IReadOnlyList<Payment>>(),
                  It.IsAny<IReadOnlyList<InventoryMovement>>(),
                  It.IsAny<OutboxEvent>(),
                  It.IsAny<CancellationToken>()))
              .Callback((Invoice invoice, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                         IReadOnlyList<InventoryMovement> _, OutboxEvent outboxEvent, CancellationToken _) =>
              {
                  _savedInvoice = invoice;
                  _savedEvent = outboxEvent;
              })
              .ReturnsAsync((Invoice invoice, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                             IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => invoice);

        _catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([new PaymentMethod { Code = "Cash", DisplayName = "Cash", IsEnabled = true, StoreId = StoreId }]);
    }

    private CompleteSaleCommandHandler Handler() => new(
        _sales.Object,
        new Mock<IProductRepository>().Object,
        MockStoreIdentityService.GeneralHardware(),
        _catalog.Object,
        NullLogger<CompleteSaleCommandHandler>.Instance);

    private static CompleteSaleCommand CashSale(string method = "Cash") =>
        new(StoreId, Guid.NewGuid(), Lines: [], Payments: [new SalePaymentRequest(method, 0m)]);

    [Fact]
    public async Task HandleAsync_WhenPaymentMethodNotOfferable_DoesNotReserveANumber()
    {
        var act = () => Handler().HandleAsync(CashSale(UnofferableMethod));

        await act.Should()
                 .ThrowAsync<SaleValidationException>();
        _sales.Verify(s => s.ReserveInvoiceNumberAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithAReservedNumber_StampsItOnTheInvoice()
    {
        await Handler().HandleAsync(CashSale());

        _savedInvoice!.InvoiceNumber.Should()
                                    .Be(ReservedNumber);
    }

    [Fact]
    public async Task HandleAsync_WithAReservedNumber_CarriesItInTheInvoiceCompletedPayload()
    {
        await Handler().HandleAsync(CashSale());

        JsonSerializer.Deserialize<InvoiceCompletedEvent>(_savedEvent!.PayloadJson)!
                      .InvoiceNumber.Should()
                                    .Be(ReservedNumber);
    }

    [Fact]
    public async Task HandleAsync_WithAReservedNumber_ReturnsIt()
    {
        var response = await Handler().HandleAsync(CashSale());

        response.InvoiceNumber.Should()
                              .Be(ReservedNumber);
    }
}
