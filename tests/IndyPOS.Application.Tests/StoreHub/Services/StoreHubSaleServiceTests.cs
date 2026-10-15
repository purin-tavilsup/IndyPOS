using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Domain.Events;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>
/// v4 receipts printed bill 0000000000 (the legacy Id was set to 0 and the response's number
/// ignored), and in RELEASE threw KeyNotFoundException on every payment (the printer indexed the
/// legacy 1–7 payment dictionary with PaymentTypeId, which v4 payments leave at 0).
/// </summary>
public class StoreHubSaleServiceTests
{
    private const long BillNumber = 1042;
    private const string UnknownCode = "NewCampaign2027";
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<IEventAggregator> _events = new();
    private readonly Mock<IPaymentMethodNames> _names = new();
    private readonly UserLoggedInEvent _loggedIn = new();
    private readonly StoreHubSaleService _sut;

    public StoreHubSaleServiceTests()
    {
        _events.Setup(e => e.GetEvent<UserLoggedInEvent>()).Returns(_loggedIn);
        _events.Setup(e => e.GetEvent<UserLoggedOutEvent>()).Returns(new UserLoggedOutEvent());
        _events.Setup(e => e.GetEvent<InvoiceProductAddedEvent>()).Returns(new InvoiceProductAddedEvent());
        _events.Setup(e => e.GetEvent<InvoicePaymentAddedEvent>()).Returns(new InvoicePaymentAddedEvent());
        _client.SetupGet(c => c.IsAuthenticated).Returns(true);
        _client.Setup(c => c.CompleteSaleAsync(It.IsAny<CompleteSaleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new CompleteSaleResponse(Guid.NewGuid(), 100m, DateTime.UtcNow, BillNumber));
        _names.Setup(n => n.GetAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Cash"] = "เงินสด" });

        _sut = new StoreHubSaleService(_client.Object, new Mock<IProductCacheService>().Object, _events.Object,
                                       _names.Object, new ReceiptDocumentFactory(Bangkok),
                                       NullLogger<StoreHubSaleService>.Instance);
        _loggedIn.Publish(Cashier());
    }

    [Fact]
    public async Task CompleteSaleAsync_WithAPaymentCodeNotInTheCatalog_PrintsTheCode()
    {
        RingUpOneProductPaidWith(UnknownCode);

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Payments.Single().DisplayName.Should()
                                             .Be(UnknownCode);
    }

    // Anything that can fail must fail before StoreHub saves the sale: a failure after it shows
    // "save failed", keeps the cart, and the cashier saves the same sale twice.
    [Fact]
    public async Task CompleteSaleAsync_WhenTheNameLookupFails_DoesNotSaveTheSale()
    {
        _names.Setup(n => n.GetAsync(It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("lookup failed"));
        RingUpOneProductPaidWith("Cash");

        await Record.ExceptionAsync(() => _sut.CompleteSaleAsync());

        _client.Verify(c => c.CompleteSaleAsync(It.IsAny<CompleteSaleRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteSaleAsync_WithAResponse_PrintsItsBillNumber()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.InvoiceNumber.Should()
                             .Be(BillNumber);
    }

    [Fact]
    public async Task CompleteSaleAsync_WithACatalogPayment_PrintsItsDisplayName()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Payments.Single().DisplayName.Should()
                                             .Be("เงินสด");
    }

    [Fact]
    public async Task CompleteSaleAsync_WithALoggedInCashier_PrintsTheirName()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.CashierName.Should()
                           .Be("Malee Cashier");
    }

    [Fact]
    public async Task CompleteSaleAsync_WithOverpayment_PrintsTheChange()
    {
        RingUpOneProductPaidWith("Cash", amount: 500m);

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Change.Should()
                      .Be(400m);
    }

    [Fact]
    public async Task CompleteSaleAsync_WithAProduct_PrintsItAsAnItem()
    {
        RingUpOneProductPaidWith("Cash");

        var receipt = await _sut.CompleteSaleAsync();

        receipt.Items.Should()
                     .Equal(new ReceiptItem("Nails 1kg", null, 1, 100m, 100m));
    }

    private void RingUpOneProductPaidWith(string methodCode, decimal amount = 100m)
    {
        _sut.AddProduct(new InventoryProductDto { Id = Guid.NewGuid(), Description = "Nails 1kg", UnitPrice = 100m });
        _sut.AddPayment(methodCode, amount, string.Empty);
    }

    private static ILoggedInUser Cashier()
    {
        var cashier = new Mock<ILoggedInUser>();
        cashier.SetupGet(u => u.FirstName).Returns("Malee");
        cashier.SetupGet(u => u.LastName).Returns("Cashier");

        return cashier.Object;
    }
}
