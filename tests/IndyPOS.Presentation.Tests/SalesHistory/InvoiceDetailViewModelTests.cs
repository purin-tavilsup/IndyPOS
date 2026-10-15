using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Receipts;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.Domain.Enums;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class InvoiceDetailViewModelTests
{
    // Now is 10:00 on 2026-10-15 in Bangkok.
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTime TodayUtc = new(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime YesterdayUtc = new(2026, 10, 14, 2, 0, 0, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly SalesHistoryAccess Cashier = new("Malee Cashier", CanViewAnyDay: false, CanReprint: true);
    private static readonly SalesHistoryAccess Manager = new("Somchai Manager", CanViewAnyDay: true, CanReprint: true);
    private static readonly SalesHistoryAccess NoReprint = new("Viewer", CanViewAnyDay: true, CanReprint: false);

    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<IReceiptPrinterService> _printer = new();

    [Fact]
    public void Reprint_WithoutSalesReprint_CannotExecute()
    {
        var vm = Detail(NoReprint);
        vm.Show(Bill(TodayUtc));

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeFalse();
    }

    [Fact]
    public void Reprint_ForATodayOnlyUserOnYesterdaysBill_CannotExecute()
    {
        var vm = Detail(Cashier);
        vm.Show(Bill(YesterdayUtc));

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeFalse();
    }

    [Fact]
    public void Reprint_WithNoBill_CannotExecute()
    {
        var vm = Detail(Manager);

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeFalse();
    }

    [Fact]
    public async Task Reprint_WhenStoreHubFails_PrintsNothing()
    {
        _client.Setup(c => c.CreateReprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("ดูได้เฉพาะบิลของวันนี้"));
        var vm = Detail(Manager);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        _printer.Verify(p => p.PrintReceipt(It.IsAny<ReceiptDocument>()), Times.Never);
    }

    [Fact]
    public async Task Reprint_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.CreateReprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("ดูได้เฉพาะบิลของวันนี้"));
        var vm = Detail(Manager);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceDetailViewModel.ReprintFailed, "ดูได้เฉพาะบิลของวันนี้"));
    }

    [Fact]
    public async Task Reprint_WhenThePrinterFails_SaysTheReprintWasRecorded()
    {
        SetupReprintOf(Bill(TodayUtc));
        _printer.Setup(p => p.PrintReceipt(It.IsAny<ReceiptDocument>()))
                .Throws(new InvalidOperationException("printer offline"));
        var vm = Detail(Manager);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceDetailViewModel.PrintFailed, "printer offline"));
    }

    [Fact]
    public async Task Load_WhenStoreHubAnswersNotFound_ShowsBillNotFound()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((InvoiceDetailDto?)null);
        var vm = Detail(Cashier);

        await vm.LoadAsync(Guid.NewGuid());

        vm.ErrorMessage.Should()
                       .Be(InvoiceDetailViewModel.BillNotFound);
    }

    [Fact]
    public async Task Load_WhenStoreHubFails_ShowsItsReason()
    {
        _client.Setup(c => c.GetSaleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubException("Cannot connect to StoreHub"));
        var vm = Detail(Cashier);

        await vm.LoadAsync(Guid.NewGuid());

        vm.ErrorMessage.Should()
                       .Be(string.Format(InvoiceDetailViewModel.LoadFailed, "Cannot connect to StoreHub"));
    }

    [Fact]
    public async Task Load_WhenAnEarlierLoadAnswersLast_KeepsTheLaterBill()
    {
        var slow = new TaskCompletionSource<InvoiceDetailDto?>();
        var first = Bill(TodayUtc) with { InvoiceNumber = 1 };
        var second = Bill(TodayUtc) with { InvoiceNumber = 2 };
        _client.Setup(c => c.GetSaleAsync(first.Id, It.IsAny<CancellationToken>())).Returns(slow.Task);
        _client.Setup(c => c.GetSaleAsync(second.Id, It.IsAny<CancellationToken>())).ReturnsAsync(second);
        var vm = Detail(Manager);
        var firstLoad = vm.LoadAsync(first.Id);
        await vm.LoadAsync(second.Id);

        slow.SetResult(first);
        await firstLoad;

        vm.Bill!.InvoiceNumber.Should()
                              .Be(2);
    }

    [Fact]
    public void Reprint_ForAManagerOnYesterdaysBill_CanExecute()
    {
        var vm = Detail(Manager);
        vm.Show(Bill(YesterdayUtc));

        vm.ReprintCommand.CanExecute(null).Should()
                                          .BeTrue();
    }

    [Fact]
    public async Task Reprint_WithABill_PrintsACopyByTheReprinter()
    {
        SetupReprintOf(Bill(TodayUtc));
        var vm = Detail(Cashier);
        vm.Show(Bill(TodayUtc));

        await vm.ReprintCommand.ExecuteAsync(null);

        _printer.Verify(p => p.PrintReceipt(It.Is<ReceiptDocument>(d => d.IsCopy && d.Reprint!.ReprintedBy == "Malee Cashier")));
    }

    [Fact]
    public void IssuedAt_WithAUtcBill_IsBangkokTime()
    {
        var vm = Detail(Manager);

        vm.Show(Bill(TodayUtc));

        vm.IssuedAt.Should()
                   .Be(new DateTime(2026, 10, 15, 9, 0, 0));
    }

    [Fact]
    public void HardwareTotal_WithMixedLines_SumsOnlyHardware()
    {
        var vm = Detail(Manager);

        vm.Show(Bill(TodayUtc));

        vm.HardwareTotal.Should()
                        .Be(250m);
    }

    [Fact]
    public void GeneralTotal_WithAnUncategorisedLine_CountsItAsGeneral()
    {
        var vm = Detail(Manager);

        vm.Show(Bill(TodayUtc));

        vm.GeneralTotal.Should()
                       .Be(130m);
    }

    private InvoiceDetailViewModel Detail(SalesHistoryAccess access) =>
        new(_client.Object, _printer.Object, new ReceiptDocumentFactory(Bangkok),
            new StoreClock(new FakeTimeProvider(NowUtc), Bangkok), access);

    private void SetupReprintOf(InvoiceDetailDto bill) =>
        _client.Setup(c => c.CreateReprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new InvoiceReprintResultDto(bill, new InvoiceReprintDto(Guid.NewGuid(), bill.Id, NowUtc.UtcDateTime, Guid.NewGuid())));

    // 250 hardware + 100 general + 30 with no category (a deleted product) = 380.
    private static InvoiceDetailDto Bill(DateTime createdUtc) => new(
        Id: Guid.NewGuid(),
        InvoiceNumber: 1042,
        StoreId: "store-a",
        UserId: Guid.NewGuid(),
        CashierName: "Malee Cashier",
        TotalAmount: 380m,
        AmountReceived: 400m,
        ChangeGiven: 20m,
        IsRefund: false,
        HasPayLater: false,
        PayLaterAmount: 0m,
        CreatedUtc: createdUtc,
        Lines:
        [
            Line("Hammer", ProductCategoryKind.Hardware, 250m),
            Line("Rice", ProductCategoryKind.GeneralGoods, 100m),
            Line("Old item", null, 30m)
        ],
        Payments: [new PaymentDto(Guid.NewGuid(), "Cash", "เงินสด", 400m, null)]);

    private static InvoiceLineDto Line(string name, ProductCategoryKind? kind, decimal total) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, null, null, kind, 1, total, total);
}
