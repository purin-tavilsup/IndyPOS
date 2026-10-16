using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Infrastructure.Services.RawDeviceInput;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.Report;
using Moq;
using Xunit;
using static IndyPOS.Windows.Forms.Tests.UI.Report.SalesHistoryFixture;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

public class InvoiceHistoryViewTests
{
    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public void Bind_ForATodayOnlyList_HidesThePeriodPicker()
    {
        using var view = new InvoiceHistoryView();

        view.Bind(Factory(_client.Object, LoggedIn(UserRole.Cashier)).CreateHistory());

        Find(view, "ShowThisMonthButton").Visible.Should()
                                                 .BeFalse();
    }

    [Fact]
    public async Task Bind_ToASecondViewModel_IgnoresTheFirstsBills()
    {
        ListsToday(Page(hasMore: false, 1042));
        using var view = new InvoiceHistoryView();
        var factory = Factory(_client.Object, LoggedIn(UserRole.StoreManager));
        var first = factory.CreateHistory();
        view.Bind(first);
        view.Bind(factory.CreateHistory());

        await first.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows.Count.Should()
                              .Be(0);
    }

    [Fact]
    public void Search_WithTextThatIsNotANumber_ShowsTheError()
    {
        using var view = new InvoiceHistoryView();
        view.Bind(Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory());
        Find(view, "BillNumberTextBox").Text = "abc";

        ((Button)Find(view, "SearchButton")).PerformClick();

        Find(view, "HistoryErrorLabel").Text.Should()
                                            .Be(InvoiceHistoryViewModel.NotANumber);
    }

    // The till installs a message filter that swallows every Enter WM_KEYDOWN (the barcode scanner ends each
    // scan with Enter). Posted messages go through the real message loop and that filter, as a key press does.
    [Fact]
    public void Search_WhenEnterIsPressedUnderTheTillsKeyFilter_SearchesTheBillNumber()
    {
        var filter = new PreMessageFilter();
        System.Windows.Forms.Application.AddMessageFilter(filter);
        try
        {
            using var form = OffScreenForm();
            var view = new InvoiceHistoryView { Dock = DockStyle.Fill };
            form.Controls.Add(view);
            view.Bind(Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory());
            form.Show();
            var box = Find(view, "BillNumberTextBox");
            box.Text = "1042";

            PressEnter(box);

            _client.Verify(c => c.GetSaleByNumberAsync(1042, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            System.Windows.Forms.Application.RemoveMessageFilter(filter);
        }
    }

    [Fact]
    public async Task ShowToday_AfterAnotherList_ReplacesTheRows()
    {
        _client.SetupSequence(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1042, 1041))
               .ReturnsAsync(Page(hasMore: false, 7));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows.Count.Should()
                              .Be(1);
    }

    [Fact]
    public async Task ShowToday_WithBills_ListsEach()
    {
        ListsToday(Page(hasMore: false, 1042, 1041));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows.Count.Should()
                              .Be(2);
    }

    [Fact]
    public async Task ShowToday_WithABill_ShowsItsNumberAsOnTheReceipt()
    {
        ListsToday(Page(hasMore: false, 1042));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);

        await vm.ShowTodayCommand.ExecuteAsync(null);

        Bills(view).Rows[0].Cells[0].FormattedValue.Should()
                                                   .Be("0000001042");
    }

    [Fact]
    public async Task LoadMore_WithAnotherPage_AppendsIt()
    {
        ListsToday(Page(hasMore: true, 1042));
        _client.Setup(c => c.ListSalesAsync(Today, Today, 2, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Page(hasMore: false, 1041));
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        ((Button)Find(view, "LoadMoreButton")).PerformClick();

        Bills(view).Rows.Count.Should()
                              .Be(2);
    }

    [Fact]
    public async Task ClickingABill_OpensIt()
    {
        var page = Page(hasMore: false, 1042);
        ListsToday(page);
        using var view = new InvoiceHistoryView();
        var vm = Factory(_client.Object, LoggedIn(UserRole.StoreManager)).CreateHistory();
        view.Bind(vm);
        await vm.ShowTodayCommand.ExecuteAsync(null);

        ClickRow(Bills(view), 0);

        _client.Verify(c => c.GetSaleAsync(page.Items[0].Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void ListsToday(SalesPage page) =>
        _client.Setup(c => c.ListSalesAsync(Today, Today, 1, InvoiceHistoryViewModel.PageSize, It.IsAny<CancellationToken>()))
               .ReturnsAsync(page);

    private static DataGridView Bills(Control view) => (DataGridView)Find(view, "BillsGrid");

    private static Form OffScreenForm() => new()
    {
        StartPosition = FormStartPosition.Manual,
        Location = new System.Drawing.Point(-3000, -3000),
        Size = new System.Drawing.Size(1400, 800),
        ShowInTaskbar = false
    };

    private const int KeyDownMessage = 0x0100;
    private const int KeyUpMessage = 0x0101;
    private const int ReturnKey = 0x0D;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private static void PressEnter(Control target)
    {
        PostMessage(target.Handle, KeyDownMessage, ReturnKey, IntPtr.Zero);
        PostMessage(target.Handle, KeyUpMessage, ReturnKey, unchecked((IntPtr)0xC0000001));
        for (var i = 0; i < 10; i++)
            System.Windows.Forms.Application.DoEvents();
    }

    // DataGridView raises CellClick only from a real mouse; this is the same event, without a window.
    private static void ClickRow(DataGridView grid, int row) =>
        typeof(DataGridView).GetMethod("OnCellClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                            .Invoke(grid, [new DataGridViewCellEventArgs(0, row)]);
}
