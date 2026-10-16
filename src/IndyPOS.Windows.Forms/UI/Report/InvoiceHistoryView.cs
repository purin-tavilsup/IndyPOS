using System.Collections.Specialized;
using System.ComponentModel;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.ModernUI;
using IndyPOS.Windows.Forms.UI.Mvvm;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// The bill list (spec §7): period buttons, a date range and the bill-number box on top, the bills on
/// the left with "load more", and the chosen bill on the right. It shows
/// <see cref="InvoiceHistoryViewModel"/>'s state and forwards clicks; the ViewModel decides everything.
/// </summary>
public sealed class InvoiceHistoryView : UserControl
{
    public const string TodayOnlyTitle = "บิลวันนี้";

    private static readonly Size PeriodButtonSize = new(120, 50);

    private readonly ModernButton _today = TillStyle.Button("ShowTodayButton", "วันนี้", PeriodButtonSize);
    private readonly ModernButton _thisMonth = TillStyle.Button("ShowThisMonthButton", "เดือนนี้", PeriodButtonSize);
    private readonly ModernButton _thisYear = TillStyle.Button("ShowThisYearButton", "ปีนี้", PeriodButtonSize);
    private readonly DateTimePicker _from = DatePicker("FromDatePicker");
    private readonly DateTimePicker _to = DatePicker("ToDatePicker");
    private readonly ModernButton _range = TillStyle.Button("ShowRangeButton", "แสดง", PeriodButtonSize);
    private readonly TextBox _billNumber = new()
    {
        Name = "BillNumberTextBox",
        Width = 180,
        Margin = new Padding(20, 12, 3, 3),
        Font = new Font(TillStyle.FontName, 14F)
    };
    private readonly ModernButton _search = TillStyle.Button("SearchButton", "ค้นหาบิล", PeriodButtonSize);
    private readonly Label _period = TillStyle.Label("PeriodLabel", 14F);
    private readonly Label _error = TillStyle.Label("HistoryErrorLabel");
    private readonly DataGridView _bills = TillGrid.Create("BillsGrid");
    private readonly ModernButton _loadMore = TillStyle.Button("LoadMoreButton", "โหลดเพิ่ม", PeriodButtonSize);
    private readonly InvoiceDetailView _detail = new() { Dock = DockStyle.Fill };
    private readonly List<IDisposable> _commandBindings = [];

    private InvoiceHistoryViewModel? _viewModel;

    public InvoiceHistoryView()
    {
        Name = nameof(InvoiceHistoryView);
        BackColor = TillStyle.Background;
        _error.ForeColor = Color.Salmon;

        TillGrid.AddColumn(_bills, "เลขที่บิล", 140, "0000000000");
        TillGrid.AddColumn(_bills, "เวลา", 170, "dd/MM/yyyy HH:mm");
        TillGrid.AddColumn(_bills, "ยอดรวม", 120, "N2");
        TillGrid.AddColumn(_bills, "ชำระโดย", 130);
        TillGrid.AddColumn(_bills, "รายการ", 70);

        _bills.CellClick += OnBillClicked;
        _billNumber.TextChanged += OnBillNumberChanged;
        _billNumber.KeyDown += OnBillNumberKeyDown;

        Controls.Add(_detail);
        Controls.Add(BillList());
        Controls.Add(StatusRow());
        Controls.Add(TopBar());
    }

    public bool ShowProductTypeSplit
    {
        get => _detail.ShowProductTypeSplit;
        set => _detail.ShowProductTypeSplit = value;
    }

    public bool IsTodayOnly => _viewModel?.IsTodayOnly == true;

    /// <summary>Follows this ViewModel from now on, and stops following the previous one.</summary>
    public void Bind(InvoiceHistoryViewModel viewModel)
    {
        Unbind();

        _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        viewModel.Bills.CollectionChanged += OnBillsChanged;
        _commandBindings.Add(CommandButton.Bind(_today, viewModel.ShowTodayCommand));
        _commandBindings.Add(CommandButton.Bind(_thisMonth, viewModel.ShowThisMonthCommand));
        _commandBindings.Add(CommandButton.Bind(_thisYear, viewModel.ShowThisYearCommand));
        _commandBindings.Add(CommandButton.Bind(_range, viewModel.ShowRangeCommand, SelectedRange));
        _commandBindings.Add(CommandButton.Bind(_search, viewModel.SearchCommand));
        _commandBindings.Add(CommandButton.Bind(_loadMore, viewModel.LoadMoreCommand));
        _detail.Bind(viewModel.Detail);

        foreach (var control in PeriodPicker())
            control.Visible = !viewModel.IsTodayOnly;

        _billNumber.Text = viewModel.SearchText;
        RefillBills();
        Render();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Unbind();

        base.Dispose(disposing);
    }

    private void Unbind()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelChanged;
            _viewModel.Bills.CollectionChanged -= OnBillsChanged;
        }

        _commandBindings.ForEach(binding => binding.Dispose());
        _commandBindings.Clear();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Render();

    // The ViewModel only appends a page or clears the list for a new period.
    private void OnBillsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (InvoiceRow row in e.NewItems)
                AddBill(row);
        }
        else
        {
            RefillBills();
        }

        Render();
    }

    private void OnBillClicked(object? sender, DataGridViewCellEventArgs e)
    {
        if (_viewModel is null || e.RowIndex < 0 || _bills.Rows[e.RowIndex].Tag is not InvoiceRow row)
            return;

        CommandButton.TryExecute(_viewModel.OpenCommand, row);
    }

    private void OnBillNumberChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.SearchText = _billNumber.Text;
    }

    private void OnBillNumberKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || _viewModel is null)
            return;

        e.SuppressKeyPress = true;
        CommandButton.TryExecute(_viewModel.SearchCommand, null);
    }

    private void Render()
    {
        _period.Text = PeriodText();
        _error.Text = _viewModel?.ErrorMessage ?? string.Empty;
        UseWaitCursor = _viewModel?.IsBusy == true;
    }

    private string PeriodText()
    {
        if (_viewModel is null)
            return string.Empty;
        if (_viewModel.IsTodayOnly)
            return TodayOnlyTitle;
        if (_viewModel.From == default)
            return string.Empty;

        return _viewModel.From == _viewModel.To
            ? $"{_viewModel.From:dd/MM/yyyy}"
            : $"{_viewModel.From:dd/MM/yyyy} - {_viewModel.To:dd/MM/yyyy}";
    }

    private void RefillBills()
    {
        _bills.Rows.Clear();

        foreach (var row in _viewModel?.Bills ?? [])
            AddBill(row);
    }

    private void AddBill(InvoiceRow row)
    {
        var index = _bills.Rows.Add(row.InvoiceNumber, row.CreatedAt, row.TotalAmount, row.PaymentMethodName, row.LineCount);
        _bills.Rows[index].Tag = row;
    }

    private object? SelectedRange() =>
        new DateRange(DateOnly.FromDateTime(_from.Value), DateOnly.FromDateTime(_to.Value));

    private Control[] PeriodPicker() => [_thisMonth, _thisYear, _from, _to, _range];

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
        bar.Controls.AddRange([_today, _thisMonth, _thisYear, _from, _to, _range, _billNumber, _search]);

        return bar;
    }

    private Panel StatusRow()
    {
        var row = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = TillStyle.Background, Padding = new Padding(10, 0, 10, 0) };
        _period.Dock = DockStyle.Left;
        _period.Width = 420;
        _error.Dock = DockStyle.Fill;
        row.Controls.Add(_error);
        row.Controls.Add(_period);

        return row;
    }

    private Panel BillList()
    {
        var list = new Panel { Dock = DockStyle.Left, Width = 650, BackColor = TillStyle.Background, Padding = new Padding(10, 5, 5, 10) };
        _loadMore.Dock = DockStyle.Bottom;
        list.Controls.Add(_bills);
        list.Controls.Add(_loadMore);

        return list;
    }

    private static DateTimePicker DatePicker(string name) => new()
    {
        Name = name,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Width = 150,
        Margin = new Padding(10, 12, 3, 3),
        Font = new Font(TillStyle.FontName, 14F),
        Value = DateTime.Today
    };
}
