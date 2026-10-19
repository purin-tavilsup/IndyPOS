using System.Collections.Specialized;
using System.ComponentModel;
using IndyPOS.Domain.Enums;
using IndyPOS.Presentation.CashDrawer;
using IndyPOS.Windows.Forms.UI.ModernUI;
using IndyPOS.Windows.Forms.UI.Mvvm;
using IndyPOS.Windows.Forms.UI.Report;

namespace IndyPOS.Windows.Forms.UI.CashDrawer;

/// <summary>
/// One cash-entry list (รายจ่าย, เงินทอน or ลูกค้าชำระหนี้): its rows, the amount / text / category inputs and
/// add, edit and delete. It shows <see cref="CashEntriesViewModel"/>'s state and forwards input; the
/// ViewModel decides everything. Control names start with <c>namePrefix</c> (e.g. "Payout") so UI
/// Automation can tell the three lists apart.
/// </summary>
public sealed class CashEntriesView : UserControl
{
    public const string General = "เบ็ดเตล็ด";
    public const string Hardware = "ฮาร์ดแวร์";

    private static readonly Size ButtonSize = new(100, 45);

    private readonly Label _title;
    private readonly DataGridView _rows;
    private readonly TextBox _amount;
    private readonly TextBox _text;
    private readonly ComboBox _category;
    private readonly ModernButton _add;
    private readonly ModernButton _edit;
    private readonly ModernButton _delete;
    private readonly Label _error;
    private readonly List<IDisposable> _commandBindings = [];

    private CashEntriesViewModel? _viewModel;
    private bool _rendering;

    public CashEntriesView(string namePrefix, string title, string textHeader)
    {
        Name = $"{namePrefix}sView";
        BackColor = TillStyle.Background;

        _title = TillStyle.Label($"{namePrefix}sTitle", 14F);
        _title.Text = title;
        _title.Dock = DockStyle.Top;
        _rows = TillGrid.Create($"{namePrefix}sGrid");
        _amount = Box($"{namePrefix}AmountBox", 120);
        _text = Box($"{namePrefix}TextBox", 220);
        _category = new ComboBox
        {
            Name = $"{namePrefix}CategoryBox",
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 130,
            Margin = new Padding(3, 8, 3, 3),
            Font = new Font(TillStyle.FontName, 14F)
        };
        _category.Items.AddRange([General, Hardware]);
        _add = TillStyle.Button($"Add{namePrefix}Button", "เพิ่ม", ButtonSize);
        _edit = TillStyle.Button($"Edit{namePrefix}Button", "แก้ไข", ButtonSize);
        _delete = TillStyle.Button($"Delete{namePrefix}Button", "ลบ", ButtonSize);
        _error = TillStyle.Label($"{namePrefix}ErrorLabel");
        _error.ForeColor = Color.Salmon;
        _error.Dock = DockStyle.Bottom;

        TillGrid.AddColumn(_rows, "เวลา", 80, "HH:mm");
        TillGrid.AddColumn(_rows, textHeader, 220);
        TillGrid.AddColumn(_rows, "หมวด", 110);
        TillGrid.AddColumn(_rows, "จำนวนเงิน", 120, "N2");

        _rows.CellClick += OnRowClicked;
        _amount.TextChanged += (_, _) => Write(vm => vm.AmountText = _amount.Text);
        _text.TextChanged += (_, _) => Write(vm => vm.Text = string.IsNullOrEmpty(_text.Text) ? null : _text.Text);
        _category.SelectedIndexChanged += (_, _) =>
            Write(vm => vm.Category = _category.SelectedIndex == 1 ? PayoutCategory.Hardware : PayoutCategory.General);

        Controls.Add(_rows);
        Controls.Add(_error);
        Controls.Add(InputRow());
        Controls.Add(_title);
    }

    /// <summary>Follows this ViewModel from now on, and stops following the previous one.</summary>
    /// <param name="showCategory">False where the store sells no hardware, so a payout has no category.</param>
    public void Bind(CashEntriesViewModel viewModel, bool showCategory)
    {
        Unbind();

        _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        viewModel.Rows.CollectionChanged += OnRowsChanged;
        _commandBindings.Add(CommandButton.Bind(_add, viewModel.AddCommand));
        _commandBindings.Add(CommandButton.Bind(_edit, viewModel.EditCommand));
        _commandBindings.Add(CommandButton.Bind(_delete, viewModel.DeleteCommand));

        var hasCategory = viewModel.HasCategory && showCategory;
        _category.Visible = hasCategory;
        _rows.Columns[2].Visible = hasCategory;

        RefillRows();
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
            _viewModel.Rows.CollectionChanged -= OnRowsChanged;
        }

        _commandBindings.ForEach(binding => binding.Dispose());
        _commandBindings.Clear();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Render();

    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefillRows();

    private void OnRowClicked(object? sender, DataGridViewCellEventArgs e)
    {
        if (_viewModel is null || e.RowIndex < 0 || _rows.Rows[e.RowIndex].Tag is not CashEntryRow row)
            return;

        _viewModel.SelectedRow = row;
    }

    // Typing writes to the ViewModel; Render writing the ViewModel's values back must not echo.
    private void Write(Action<CashEntriesViewModel> write)
    {
        if (!_rendering && _viewModel is not null)
            write(_viewModel);
    }

    private void Render()
    {
        if (_viewModel is null)
            return;

        _rendering = true;
        try
        {
            SetText(_amount, _viewModel.AmountText);
            SetText(_text, _viewModel.Text ?? string.Empty);
            _category.SelectedIndex = _viewModel.Category == PayoutCategory.Hardware ? 1 : 0;
            _error.Text = _viewModel.ErrorMessage ?? string.Empty;
            UseWaitCursor = _viewModel.IsBusy;
        }
        finally
        {
            _rendering = false;
        }
    }

    private static void SetText(TextBox box, string text)
    {
        if (box.Text != text)
            box.Text = text;
    }

    private void RefillRows()
    {
        _rows.Rows.Clear();

        foreach (var row in _viewModel?.Rows ?? [])
        {
            var category = row.Category == PayoutCategory.Hardware ? Hardware : General;
            var index = _rows.Rows.Add(row.StoreTime, row.Text, category, row.Amount);
            _rows.Rows[index].Tag = row;
        }
    }

    private FlowLayoutPanel InputRow()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            WrapContents = false,
            BackColor = TillStyle.Background,
            Padding = new Padding(0, 4, 0, 0)
        };
        row.Controls.AddRange([_amount, _text, _category, _add, _edit, _delete]);

        return row;
    }

    private static TextBox Box(string name, int width) => new()
    {
        Name = name,
        Width = width,
        Margin = new Padding(3, 8, 3, 3),
        Font = new Font(TillStyle.FontName, 14F)
    };
}
