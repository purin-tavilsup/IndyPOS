using System.Collections.Specialized;
using System.ComponentModel;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Windows.Forms.UI.ModernUI;
using IndyPOS.Windows.Forms.UI.Mvvm;
using IndyPOS.Windows.Forms.UI.Report;

namespace IndyPOS.Windows.Forms.UI.CashDrawer;

/// <summary>
/// The cash count: nine boxes (1000 down to 1), the live total, save, reset and the day's earlier counts
/// with who counted. It shows <see cref="CashCountViewModel"/>'s state and forwards input.
/// </summary>
public sealed class CashCountView : UserControl
{
    private readonly (TextBox Box, string Caption, Func<CashCountViewModel, int> Read, Action<CashCountViewModel, int> Write)[] _counts;
    private readonly Label _typedTotal = TillStyle.Label("TypedTotalLabel", 16F);
    private readonly ModernButton _save = TillStyle.Button("SaveCountButton", "บันทึกการนับ", new Size(150, 50));
    private readonly ModernButton _reset = TillStyle.Button("ResetCountButton", "เริ่มการนับใหม่", new Size(150, 50));
    private readonly DataGridView _history = TillGrid.Create("CountHistoryGrid");
    private readonly Label _error = TillStyle.Label("CountErrorLabel");
    private readonly List<IDisposable> _commandBindings = [];

    private CashCountViewModel? _viewModel;
    private bool _rendering;

    public CashCountView()
    {
        Name = nameof(CashCountView);
        BackColor = TillStyle.Background;
        _error.ForeColor = Color.Salmon;
        _error.Dock = DockStyle.Bottom;

        _counts =
        [
            (Box("BankNote1000CountBox"), "1,000", vm => vm.BankNote1000Count, (vm, n) => vm.BankNote1000Count = n),
            (Box("BankNote500CountBox"), "500", vm => vm.BankNote500Count, (vm, n) => vm.BankNote500Count = n),
            (Box("BankNote100CountBox"), "100", vm => vm.BankNote100Count, (vm, n) => vm.BankNote100Count = n),
            (Box("BankNote50CountBox"), "50", vm => vm.BankNote50Count, (vm, n) => vm.BankNote50Count = n),
            (Box("BankNote20CountBox"), "20", vm => vm.BankNote20Count, (vm, n) => vm.BankNote20Count = n),
            (Box("Coin10CountBox"), "10", vm => vm.Coin10Count, (vm, n) => vm.Coin10Count = n),
            (Box("Coin5CountBox"), "5", vm => vm.Coin5Count, (vm, n) => vm.Coin5Count = n),
            (Box("Coin2CountBox"), "2", vm => vm.Coin2Count, (vm, n) => vm.Coin2Count = n),
            (Box("Coin1CountBox"), "1", vm => vm.Coin1Count, (vm, n) => vm.Coin1Count = n)
        ];

        foreach (var count in _counts)
            count.Box.TextChanged += (_, _) => WriteCount(count.Box, count.Write);

        TillGrid.AddColumn(_history, "เวลา", 80, "HH:mm");
        TillGrid.AddColumn(_history, "ผู้นับ", 200);
        TillGrid.AddColumn(_history, "ยอดนับ", 130, "N2");

        Controls.Add(_history);
        Controls.Add(_error);
        Controls.Add(Buttons());
        Controls.Add(CountGrid());
    }

    public void Bind(CashCountViewModel viewModel)
    {
        Unbind();

        _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        viewModel.History.CollectionChanged += OnHistoryChanged;
        _commandBindings.Add(CommandButton.Bind(_save, viewModel.SaveCommand));
        _commandBindings.Add(CommandButton.Bind(_reset, viewModel.ResetCommand));

        RefillHistory();
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
            _viewModel.History.CollectionChanged -= OnHistoryChanged;
        }

        _commandBindings.ForEach(binding => binding.Dispose());
        _commandBindings.Clear();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Render();

    private void OnHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefillHistory();

    // Input shape only, as v3 did: anything that is not a whole number counts as 0.
    private void WriteCount(TextBox box, Action<CashCountViewModel, int> write)
    {
        if (_rendering || _viewModel is null)
            return;

        write(_viewModel, int.TryParse(box.Text, out var count) ? count : 0);
    }

    private void Render()
    {
        if (_viewModel is null)
            return;

        _rendering = true;
        try
        {
            foreach (var count in _counts)
                ShowCount(count.Box, count.Read(_viewModel));

            _typedTotal.Text = $"{_viewModel.TypedTotal:N2}";
            _error.Text = _viewModel.ErrorMessage ?? string.Empty;
            UseWaitCursor = _viewModel.IsBusy;
        }
        finally
        {
            _rendering = false;
        }
    }

    // Leaves what the cashier is typing alone (e.g. "abc" stays while the ViewModel holds 0); only a real
    // change from the ViewModel, such as Reset, rewrites the box.
    private static void ShowCount(TextBox box, int count)
    {
        var shown = int.TryParse(box.Text, out var typed) ? typed : 0;
        if (shown != count)
            box.Text = count.ToString();
    }

    private void RefillHistory()
    {
        _history.Rows.Clear();

        foreach (var row in _viewModel?.History ?? [])
            _history.Rows.Add(row.StoreTime, row.CountedByName ?? string.Empty, row.CountedTotal);
    }

    private TableLayoutPanel CountGrid()
    {
        const int rowHeight = 36;
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = (_counts.Length + 1) * rowHeight + 6,
            ColumnCount = 2,
            RowCount = _counts.Length + 1,
            BackColor = TillStyle.Background
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));

        foreach (var count in _counts)
            AddRow(grid, $"{count.Box.Name}Caption", count.Caption, count.Box, rowHeight);

        _typedTotal.Dock = DockStyle.Fill;
        AddRow(grid, "TypedTotalCaption", "รวม", _typedTotal, rowHeight);

        return grid;
    }

    private static void AddRow(TableLayoutPanel grid, string captionName, string captionText, Control field, int rowHeight)
    {
        var caption = TillStyle.Label(captionName, 14F);
        caption.Text = captionText;
        caption.TextAlign = ContentAlignment.MiddleRight;
        caption.Dock = DockStyle.Fill;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        grid.Controls.Add(caption);
        grid.Controls.Add(field);
    }

    private FlowLayoutPanel Buttons()
    {
        var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60, WrapContents = false, BackColor = TillStyle.Background };
        row.Controls.AddRange([_save, _reset]);

        return row;
    }

    private static TextBox Box(string name) => new()
    {
        Name = name,
        Width = 100,
        Text = "0",
        TextAlign = HorizontalAlignment.Right,
        Font = new Font(TillStyle.FontName, 14F)
    };
}
