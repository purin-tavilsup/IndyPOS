using System.Collections.Specialized;
using System.ComponentModel;
using IndyPOS.Application.Common.Models;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI.ModernUI;
using IndyPOS.Windows.Forms.UI.Mvvm;
using IndyPOS.Windows.Forms.UI.Report;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.CashDrawer;

/// <summary>
/// ลิ้นชักเก็บเงิน (cash spec §8): the day's figures on the left, the three lists in the middle, the count
/// and today's ลงบัญชี on the right. Every visit gets a fresh ViewModel for whoever is logged in now; the
/// panel only shows its state and forwards input.
/// </summary>
public sealed class CashDrawerPanel : UserControl
{
    private readonly CashDrawerViewModelFactory _factory;
    private readonly IStoreFeaturesProvider _features;

    private readonly ModernButton _refresh = TillStyle.Button("RefreshCashDrawerButton", "ดึงข้อมูลล่าสุด", new Size(170, 50));
    private readonly DateTimePicker _dayPicker = new()
    {
        Name = "CashDrawerDayPicker",
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Width = 150,
        Margin = new Padding(10, 12, 3, 3),
        Font = new Font(TillStyle.FontName, 14F)
    };
    private readonly ModernButton _showDay = TillStyle.Button("ShowCashDrawerDayButton", "แสดง", new Size(100, 50));
    private readonly Label _day = TillStyle.Label("CashDrawerDayLabel", 14F);
    private readonly Label _error = TillStyle.Label("CashDrawerErrorLabel");
    private readonly FlowLayoutPanel _summaryRows = new() { Name = "CashSummaryRows", Dock = DockStyle.Fill, BackColor = TillStyle.Background };
    private readonly Label _expectedCash = TillStyle.Label("ExpectedCashLabel", 16F);
    private readonly Label _countedCash = TillStyle.Label("CountedCashLabel", 16F);
    private readonly Label _difference = TillStyle.Label("CashDifferenceLabel", 16F);
    private readonly CashEntriesView _payouts = new("Payout", "รายจ่าย", "รายการ") { Dock = DockStyle.Top, Height = 330 };
    private readonly CashEntriesView _floats = new("Float", "เงินทอน", "หมายเหตุ") { Dock = DockStyle.Top, Height = 290 };
    private readonly CashEntriesView _debtRepayments = new("DebtRepayment", "ลูกค้าชำระหนี้", "ชื่อลูกค้า") { Dock = DockStyle.Top, Height = 290 };
    private readonly CashCountView _count = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _todaysPayLater = TillGrid.Create("TodaysPayLaterGrid");
    private readonly List<IDisposable> _commandBindings = [];

    public CashDrawerPanel(CashDrawerViewModelFactory factory, IStoreFeaturesProvider features)
    {
        _factory = factory;
        _features = features;

        Name = nameof(CashDrawerPanel);
        BackColor = TillStyle.Background;
        _error.ForeColor = Color.Salmon;
        TillGrid.AddColumn(_todaysPayLater, "เวลา", 80, "HH:mm");
        TillGrid.AddColumn(_todaysPayLater, "ลงบัญชี", 200);
        TillGrid.AddColumn(_todaysPayLater, "จำนวนเงิน", 120, "N2");

        Controls.Add(Columns());
        Controls.Add(TopBar());
        VisibleChanged += OnVisibilityChanged;
    }

    /// <summary>The ViewModel this visit shows; null until opened, or when nobody is logged in.</summary>
    public CashDrawerViewModel? ViewModel { get; private set; }

    public async Task OpenAsync()
    {
        CashDrawerViewModel viewModel;
        try
        {
            viewModel = _factory.Create(await FeaturesAsync());
        }
        catch (InvalidOperationException ex)
        {
            // Only reachable through a stale event after a logout; the screen stays empty.
            Log.Warning(ex, "Cash drawer opened with nobody logged in");
            return;
        }

        Bind(viewModel);
        await viewModel.RefreshCommand.ExecuteAsync(null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Unbind();

        base.Dispose(disposing);
    }

    // Features that cannot be read show everything, as the rest of the till does (the ViewModel's rule).
    private async Task<StoreFeaturesDto?> FeaturesAsync()
    {
        try
        {
            return await _features.GetAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for the cash drawer");
            return null;
        }
    }

    private void Bind(CashDrawerViewModel viewModel)
    {
        Unbind();

        ViewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        viewModel.TodaysPayLater.CollectionChanged += OnPayLaterChanged;
        _commandBindings.Add(CommandButton.Bind(_refresh, viewModel.RefreshCommand));
        _commandBindings.Add(CommandButton.Bind(_showDay, viewModel.ShowDayCommand, () => DateOnly.FromDateTime(_dayPicker.Value)));

        _payouts.Bind(viewModel.Payouts, viewModel.ShowPayoutCategory);
        _floats.Bind(viewModel.Floats, showCategory: false);
        _debtRepayments.Bind(viewModel.DebtRepayments, showCategory: false);
        _count.Bind(viewModel.Count);

        _debtRepayments.Visible = viewModel.ShowPayLater;
        _todaysPayLater.Visible = viewModel.ShowPayLater;
        _dayPicker.Visible = _showDay.Visible = viewModel.CanPickDay;

        RefillPayLater();
        Render();
    }

    private void Unbind()
    {
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelChanged;
            ViewModel.TodaysPayLater.CollectionChanged -= OnPayLaterChanged;
        }

        _commandBindings.ForEach(binding => binding.Dispose());
        _commandBindings.Clear();
    }

    private async void OnVisibilityChanged(object? sender, EventArgs e)
    {
        if (Visible)
            await OpenAsync();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Render();

    private void OnPayLaterChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefillPayLater();

    private void Render()
    {
        if (ViewModel is null)
            return;

        var summary = ViewModel.Summary;
        _day.Text = $"{ViewModel.BusinessDate:dd/MM/yyyy}";
        _error.Text = ViewModel.ErrorMessage ?? string.Empty;
        _expectedCash.Text = summary is null ? string.Empty : $"{summary.ExpectedCash:N2}";
        _countedCash.Text = summary?.CountedCash is { } counted ? $"{counted:N2}" : string.Empty;
        _difference.Text = summary?.CashDifference is { } difference ? $"{difference:N2} {ViewModel.DifferenceText}" : string.Empty;
        UseWaitCursor = ViewModel.IsBusy;
        MoneyRowView.Replace(_summaryRows, ViewModel.SummaryRows, rowsPerLine: 1);
    }

    private void RefillPayLater()
    {
        _todaysPayLater.Rows.Clear();

        foreach (var row in ViewModel?.TodaysPayLater ?? [])
            _todaysPayLater.Rows.Add(row.StoreTime, row.Description, row.Amount);
    }

    private FlowLayoutPanel TopBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 70,
            WrapContents = false,
            BackColor = TillStyle.Background,
            Padding = new Padding(10, 8, 10, 0)
        };
        _day.Width = 160;
        _error.Width = 700;
        bar.Controls.AddRange([_refresh, _dayPicker, _showDay, _day, _error]);

        return bar;
    }

    // Shares of the width, not pixels: the panel is as wide as the till's screen leaves it, and scales with it.
    private TableLayoutPanel Columns()
    {
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = TillStyle.Background };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        columns.Controls.Add(SummaryColumn(), 0, 0);
        columns.Controls.Add(ListsColumn(), 1, 0);
        columns.Controls.Add(CountColumn(), 2, 0);

        return columns;
    }

    private Panel SummaryColumn()
    {
        var column = new Panel { Dock = DockStyle.Fill, BackColor = TillStyle.Background, Padding = new Padding(5) };
        var cash = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 150, ColumnCount = 2, RowCount = 3, BackColor = TillStyle.Background };
        cash.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        cash.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        foreach (var (caption, figure) in new[] { ("เงินสด [ คำนวณ ]", _expectedCash), ("เงินสด [ นับ ]", _countedCash), ("ผลต่าง", _difference) })
        {
            cash.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            figure.Dock = DockStyle.Fill;
            figure.TextAlign = ContentAlignment.MiddleRight;
            cash.Controls.Add(Caption(caption));
            cash.Controls.Add(figure);
        }

        column.Controls.Add(_summaryRows);
        column.Controls.Add(cash);

        return column;
    }

    private Panel ListsColumn()
    {
        var column = new Panel { Dock = DockStyle.Fill, BackColor = TillStyle.Background, Padding = new Padding(5), AutoScroll = true };
        column.Controls.Add(_debtRepayments);
        column.Controls.Add(_floats);
        column.Controls.Add(_payouts);

        return column;
    }

    private Panel CountColumn()
    {
        var column = new Panel { Dock = DockStyle.Fill, BackColor = TillStyle.Background, Padding = new Padding(5) };
        _todaysPayLater.Dock = DockStyle.Bottom;
        _todaysPayLater.Height = 220;
        column.Controls.Add(_count);
        column.Controls.Add(_todaysPayLater);

        return column;
    }

    private static Label Caption(string text)
    {
        var label = TillStyle.Label($"{text}Caption", 14F);
        label.Text = text;
        label.Dock = DockStyle.Fill;
        return label;
    }
}
