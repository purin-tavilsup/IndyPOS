using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.Report;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

public class InvoiceDetailViewTests
{
    // Now is 10:00 on 2026-10-15 in Bangkok; the bills below are 09:00 today and 09:00 yesterday.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTime TodayUtc = new(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime YesterdayUtc = new(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly SalesHistoryAccess Cashier = new("Malee Cashier", CanViewAnyDay: false, CanReprint: true);
    private static readonly SalesHistoryAccess Manager = new("Somchai Manager", CanViewAnyDay: true, CanReprint: true);

    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public void Bind_ForATodayOnlyUserOnYesterdaysBill_DisablesReprint()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Cashier);
        view.Bind(vm);

        vm.Show(Bill(YesterdayUtc));

        Find(view, "ReprintButton").Enabled.Should()
                                           .BeFalse();
    }

    [Fact]
    public async Task Bind_WhenTheBillIsNotFound_ShowsTheError()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        using var view = new InvoiceDetailView();
        var vm = Detail(Cashier);
        view.Bind(vm);

        await vm.LoadAsync(Guid.NewGuid());

        Find(view, "ErrorLabel").Text.Should()
                                     .Be(InvoiceDetailViewModel.BillNotFound);
    }

    [Fact]
    public void Bind_WithNoProductTypeSplit_HidesTheSplit()
    {
        using var view = new InvoiceDetailView { ShowProductTypeSplit = false };
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc));

        Find(view, "SplitLabel").Visible.Should()
                                        .BeFalse();
    }

    [Fact]
    public void Bind_ToASecondViewModel_StopsFollowingTheFirst()
    {
        using var view = new InvoiceDetailView();
        var first = Detail(Manager);
        view.Bind(first);
        view.Bind(Detail(Manager));

        first.Show(Bill(TodayUtc) with { InvoiceNumber = 7 });

        Find(view, "BillNumberLabel").Text.Should()
                                          .NotContain("0000000007");
    }

    [Fact]
    public void Bind_ToASecondViewModel_IgnoresTheFirstsReprintState()
    {
        using var view = new InvoiceDetailView();
        var first = Detail(Manager);
        view.Bind(first);
        view.Bind(Detail(Manager));

        first.Show(Bill(TodayUtc));

        Find(view, "ReprintButton").Enabled.Should()
                                           .BeFalse();
    }

    [Fact]
    public void Bind_WithABill_ShowsItsNumberAsOnTheReceipt()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc));

        Find(view, "BillNumberLabel").Text.Should()
                                          .Contain("0000001042");
    }

    [Fact]
    public void Bind_WithABill_ListsEachLine()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc));

        ((DataGridView)Find(view, "LinesGrid")).Rows.Count.Should()
                                                          .Be(2);
    }

    [Fact]
    public void Bind_WithARefund_ShowsTheRefundBadge()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(TodayUtc) with { IsRefund = true });

        Find(view, "BadgesLabel").Text.Should()
                                      .Contain(InvoiceDetailView.RefundBadge);
    }

    [Fact]
    public void Bind_ForAManagerOnYesterdaysBill_EnablesReprint()
    {
        using var view = new InvoiceDetailView();
        var vm = Detail(Manager);
        view.Bind(vm);

        vm.Show(Bill(YesterdayUtc));

        Find(view, "ReprintButton").Enabled.Should()
                                           .BeTrue();
    }

    private InvoiceDetailViewModel Detail(SalesHistoryAccess access) =>
        new(_client.Object, Mock.Of<IReceiptPrinterService>(), new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), access);

    private static InvoiceDetailDto Bill(DateTime createdUtc) => new(
        Guid.NewGuid(), 1042, "store-a", Guid.NewGuid(), "Malee Cashier", 350m, 400m, 50m, false, false, 0m, createdUtc,
        [
            new InvoiceLineDto(Guid.NewGuid(), Guid.NewGuid(), "Hammer", null, null, ProductCategoryKind.Hardware, 1, 250m, 250m),
            new InvoiceLineDto(Guid.NewGuid(), Guid.NewGuid(), "Rice", null, null, ProductCategoryKind.GeneralGoods, 1, 100m, 100m)
        ],
        [new PaymentDto(Guid.NewGuid(), "Cash", "เงินสด", 400m, null)]);

    private static Control Find(Control root, string name) => root.Controls.Find(name, searchAllChildren: true).Single();
}
