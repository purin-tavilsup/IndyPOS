using System.ComponentModel;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.UI.ModernUI;
using IndyPOS.Windows.Forms.UI.Mvvm;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// One bill (spec §7). It shows <see cref="InvoiceDetailViewModel"/>'s state and forwards พิมพ์ซ้ำ;
/// everything else lives in the ViewModel. Built in code, not the designer, so the layout is reviewable.
/// The void feature adds its button beside พิมพ์ซ้ำ.
/// </summary>
public sealed class InvoiceDetailView : UserControl
{
    public const string RefundBadge = "คืนสินค้า";
    public const string PayLaterBadge = "ลงบัญชี";

    private readonly Label _billNumber = TillStyle.Label("BillNumberLabel", 18F);
    private readonly Label _issuedAt = TillStyle.Label("IssuedAtLabel");
    private readonly Label _cashier = TillStyle.Label("CashierLabel");
    private readonly Label _badges = TillStyle.Label("BadgesLabel");
    private readonly DataGridView _lines = TillGrid.Create("LinesGrid");
    private readonly DataGridView _payments = TillGrid.Create("PaymentsGrid");
    private readonly Label _total = TillStyle.Label("TotalLabel");
    private readonly Label _received = TillStyle.Label("ReceivedLabel");
    private readonly Label _change = TillStyle.Label("ChangeLabel");
    private readonly Label _split = TillStyle.Label("SplitLabel");
    private readonly Label _error = TillStyle.Label("ErrorLabel");
    private readonly ModernButton _reprint = TillStyle.Button("ReprintButton", "พิมพ์ซ้ำ", new Size(180, 60));

    private InvoiceDetailViewModel? _viewModel;
    private IDisposable? _reprintBinding;
    private bool _showProductTypeSplit = true;

    public InvoiceDetailView()
    {
        Name = nameof(InvoiceDetailView);
        _reprint.Dock = DockStyle.Right;
        BackColor = TillStyle.Background;
        _error.ForeColor = Color.Salmon;

        TillGrid.AddColumn(_lines, "สินค้า", 320);
        TillGrid.AddColumn(_lines, "จำนวน", 90);
        TillGrid.AddColumn(_lines, "ราคาต่อหน่วย", 140, "N2");
        TillGrid.AddColumn(_lines, "ราคารวม", 140, "N2");
        TillGrid.AddColumn(_lines, "Note", 200);
        TillGrid.AddColumn(_payments, "การชำระเงิน", 320);
        TillGrid.AddColumn(_payments, "จำนวนเงิน", 160, "N2");
        TillGrid.AddColumn(_payments, "Note", 200);

        Controls.Add(_lines);
        Controls.Add(Stack(DockStyle.Bottom, 170, _payments));
        Controls.Add(Totals());
        Controls.Add(Stack(DockStyle.Top, 130, _billNumber, _issuedAt, _cashier, _badges));
    }

    /// <summary>Only GeneralHardware splits its bills (TillLayout.ShowProductTypeSplit).</summary>
    public bool ShowProductTypeSplit
    {
        get => _showProductTypeSplit;
        set
        {
            _showProductTypeSplit = value;
            Render();
        }
    }

    /// <summary>Follows this ViewModel from now on, and stops following the previous one.</summary>
    public void Bind(InvoiceDetailViewModel viewModel)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelChanged;
        _reprintBinding?.Dispose();

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _reprintBinding = CommandButton.Bind(_reprint, viewModel.ReprintCommand);
        Render();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_viewModel is not null)
                _viewModel.PropertyChanged -= OnViewModelChanged;
            _reprintBinding?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e) => Render();

    private void Render()
    {
        var bill = _viewModel?.Bill;

        _billNumber.Text = bill is null ? string.Empty : $"บิลเลขที่ {bill.InvoiceNumber:0000000000}";
        _issuedAt.Text = _viewModel?.IssuedAt is { } issuedAt ? $"{issuedAt:dd/MM/yyyy HH:mm}" : string.Empty;
        _cashier.Text = bill is null ? string.Empty : $"แคชเชียร์: {bill.CashierName ?? "-"}";
        _badges.Text = bill is null ? string.Empty : Badges(bill.IsRefund, bill.HasPayLater, bill.PayLaterAmount);
        _total.Text = bill is null ? string.Empty : $"ราคารวม {bill.TotalAmount:N2}";
        _received.Text = bill is null ? string.Empty : $"เงินรวม {bill.AmountReceived:N2}";
        _change.Text = bill is null ? string.Empty : $"เงินทอน {bill.ChangeGiven:N2}";
        _split.Visible = ShowProductTypeSplit && bill is not null;
        _split.Text = $"เบ็ดเตล็ด {_viewModel?.GeneralTotal:N2} · ฮาร์ดแวร์ {_viewModel?.HardwareTotal:N2}";
        _error.Text = _viewModel?.ErrorMessage ?? string.Empty;
        UseWaitCursor = _viewModel?.IsBusy == true;

        FillGrids();
    }

    private void FillGrids()
    {
        var bill = _viewModel?.Bill;
        _lines.Rows.Clear();
        _payments.Rows.Clear();

        if (bill is null)
            return;

        foreach (var line in bill.Lines)
            _lines.Rows.Add(line.ProductName, line.Quantity, line.UnitPrice, line.LineTotal, line.Note);

        foreach (var payment in bill.Payments)
            _payments.Rows.Add(payment.MethodDisplayName, payment.Amount, payment.Note);
    }

    private static string Badges(bool isRefund, bool hasPayLater, decimal payLaterAmount)
    {
        var badges = new List<string>();
        if (isRefund)
            badges.Add(RefundBadge);
        if (hasPayLater)
            badges.Add($"{PayLaterBadge} {payLaterAmount:N2}");

        return string.Join(" · ", badges);
    }

    private Panel Totals()
    {
        var figures = Stack(DockStyle.Fill, 0, _total, _received, _change, _split, _error);
        var totals = new Panel { Dock = DockStyle.Bottom, Height = 170, BackColor = TillStyle.Background, Padding = new Padding(10) };
        totals.Controls.Add(figures);
        totals.Controls.Add(_reprint);

        return totals;
    }

    // Children dock to the top in the order given (WinForms docks the last-added first, hence the reverse).
    private static Panel Stack(DockStyle dock, int height, params Control[] children)
    {
        var panel = new Panel { Dock = dock, BackColor = TillStyle.Background, Padding = new Padding(10, 5, 10, 5) };
        if (height > 0)
            panel.Height = height;

        // Enumerable.Reverse, not children.Reverse(): on an array, C# 14 binds the in-place span Reverse.
        foreach (var child in Enumerable.Reverse(children))
        {
            child.Dock = child is DataGridView ? DockStyle.Fill : DockStyle.Top;
            panel.Controls.Add(child);
        }

        return panel;
    }
}
