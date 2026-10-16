using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI.Report;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

/// <summary>LoadAsync only: ShowDialog would block the test run on a modal window.</summary>
public class InvoiceDetailFormTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private static readonly StoreFeaturesDto MimyMart = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false);

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    [Fact]
    public async Task LoadAsync_WhenStoreHubAnswersNotFound_ShowsBillNotFound()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        using var form = Form();

        await form.LoadAsync(Guid.NewGuid());

        Find(form, "ErrorLabel").Text.Should()
                                     .Be(InvoiceDetailViewModel.BillNotFound);
    }

    // A second double-click while the first bill is still loading must not open a second dialog: the
    // first ShowDialog would already be up, and the second would throw "Form that is already visible".
    // The first load is left pending for good, so no ShowDialog ever runs in this test.
    [Fact]
    public void ShowBillAsync_WhileABillIsLoading_IgnoresTheSecondRequest()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .Returns(new TaskCompletionSource<InvoiceDetailDto?>().Task);
        using var form = Form();
        _ = form.ShowBillAsync(Guid.NewGuid());

        _ = form.ShowBillAsync(Guid.NewGuid());

        _client.Verify(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_AtAStoreWithoutTheSplit_HidesIt()
    {
        var bill = Bill();
        _client.Setup(c => c.GetSaleAsync(bill.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bill);
        using var form = Form();

        await form.LoadAsync(bill.Id);

        // Not SplitLabel.Visible: every child of an unshown Form reports Visible == false, so that could not fail.
        ((InvoiceDetailView)Find(form, "InvoiceDetailView")).ShowProductTypeSplit.Should()
                                                                                  .BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithABill_ShowsIt()
    {
        var bill = Bill();
        _client.Setup(c => c.GetSaleAsync(bill.Id, It.IsAny<CancellationToken>())).ReturnsAsync(bill);
        using var form = Form();

        await form.LoadAsync(bill.Id);

        Find(form, "BillNumberLabel").Text.Should()
                                          .Contain("0000001042");
    }

    private InvoiceDetailForm Form()
    {
        var user = new Mock<ILoggedInUser>();
        user.SetupGet(u => u.FirstName).Returns("Somchai");
        user.SetupGet(u => u.LastName).Returns("Manager");
        user.SetupGet(u => u.RoleId).Returns((int)UserRole.StoreManager);
        _currentUser.SetupGet(c => c.User).Returns(user.Object);

        var features = new Mock<IStoreFeaturesProvider>();
        features.Setup(f => f.GetAsync()).ReturnsAsync(MimyMart);

        var factory = new SalesHistoryViewModelFactory(
            _client.Object, Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(new DateTimeOffset(2026, 10, 15, 3, 0, 0, TimeSpan.Zero)), Bangkok),
            _currentUser.Object);

        return new InvoiceDetailForm(factory, features.Object);
    }

    private static InvoiceDetailDto Bill() => new(
        Guid.NewGuid(), 1042, "store-a", Guid.NewGuid(), "Malee Cashier", 100m, 100m, 0m, false, false, 0m,
        new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc), [], []);

    private static Control Find(Control root, string name) => root.Controls.Find(name, searchAllChildren: true).Single();
}
