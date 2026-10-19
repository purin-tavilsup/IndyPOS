using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

/// <summary>
/// Spec 2026-10-19 §3: StoreHub decides which store day a sale belongs to and sends it, so the cloud never
/// converts a time zone and can never file a sale on a different day from the till.
/// </summary>
public class CompleteSaleBusinessDateTests
{
    private const string StoreId = "STORE-001";

    // 00:30 in Bangkok on 20 October is still 19 October in UTC.
    private static readonly DateTime HalfPastMidnightUtc = new(2026, 10, 19, 17, 30, 0, DateTimeKind.Utc);
    private static readonly DateOnly BangkokDay = new(2026, 10, 20);

    private readonly Mock<ISaleRepository> _sales = new();
    private readonly Mock<IPaymentMethodCatalogService> _catalog = new();
    private readonly Mock<ICashDrawerClock> _clock = new();
    private Invoice? _savedInvoice;
    private IReadOnlyList<OutboxEvent> _savedEvents = [];

    public CompleteSaleBusinessDateTests()
    {
        _clock.Setup(c => c.Now())
              .Returns(new CashDrawerInstant(HalfPastMidnightUtc, BangkokDay));

        _sales.Setup(s => s.CompleteSaleAsync(
                  It.IsAny<Invoice>(),
                  It.IsAny<IReadOnlyList<InvoiceLine>>(),
                  It.IsAny<IReadOnlyList<Payment>>(),
                  It.IsAny<IReadOnlyList<InventoryMovement>>(),
                  It.IsAny<IReadOnlyList<OutboxEvent>>(),
                  It.IsAny<CancellationToken>()))
              .Callback((Invoice invoice, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                         IReadOnlyList<InventoryMovement> _, IReadOnlyList<OutboxEvent> events, CancellationToken _) =>
              {
                  _savedInvoice = invoice;
                  _savedEvents = events;
              })
              .ReturnsAsync((Invoice invoice, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                             IReadOnlyList<InventoryMovement> _, IReadOnlyList<OutboxEvent> _, CancellationToken _) => invoice);

        _catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([new PaymentMethod { Code = "Cash", DisplayName = "Cash", IsEnabled = true, StoreId = StoreId }]);
    }

    [Fact]
    public async Task HandleAsync_WithASale_PutsTheStoresBusinessDateOnTheEvent()
    {
        await Handler().HandleAsync(CashSale());

        JsonSerializer.Deserialize<InvoiceCompletedEvent>(_savedEvents[0].PayloadJson)!.BusinessDate.Should()
                                                                                       .Be(BangkokDay);
    }

    [Fact]
    public async Task HandleAsync_WithASale_StampsTheClocksInstant()
    {
        await Handler().HandleAsync(CashSale());

        _savedInvoice!.CreatedUtc.Should()
                                 .Be(HalfPastMidnightUtc);
    }

    private CompleteSaleCommandHandler Handler() => new(
        _sales.Object,
        new Mock<IProductRepository>().Object,
        MockStoreIdentityService.GeneralHardware(),
        _catalog.Object,
        NullLogger<CompleteSaleCommandHandler>.Instance,
        _clock.Object);

    private static CompleteSaleCommand CashSale() =>
        new(StoreId, Guid.NewGuid(), Lines: [], Payments: [new SalePaymentRequest("Cash", 0m)]);
}
