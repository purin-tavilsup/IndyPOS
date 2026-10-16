using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// บิลวันนี้ (spec §7): the sales-history screen in today-only mode, opened from the sale screen, so a
/// cashier, who cannot open Reports, can find and reprint a bill from today.
/// </summary>
public sealed class TodaysBillsForm : Form
{
    private readonly SalesHistoryViewModelFactory _factory;
    private readonly IStoreFeaturesProvider _features;
    private readonly InvoiceHistoryView _view = new() { Dock = DockStyle.Fill };
    private bool _opening;

    public TodaysBillsForm(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)
    {
        _factory = factory;
        _features = features;

        Name = nameof(TodaysBillsForm);
        Text = InvoiceHistoryView.TodayOnlyTitle;
        Size = new Size(1500, 900);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = TillStyle.Background;
        Controls.Add(_view);
    }

    /// <summary>Binds a fresh today-only ViewModel and lists today, without showing the dialog.</summary>
    public async Task LoadAsync()
    {
        var viewModel = _factory.CreateTodayOnly();
        _view.ShowProductTypeSplit = (await _features.GetLayoutAsync("today's bills")).ShowProductTypeSplit;
        _view.Bind(viewModel);

        await viewModel.ShowTodayCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Lists today and shows the dialog. A press while another is still loading or shown is ignored: the
    /// dialog appears only once its list loads, so a second press would otherwise open it twice.
    /// </summary>
    public async Task ShowTodaysBillsAsync()
    {
        if (_opening)
            return;

        _opening = true;
        try
        {
            await LoadAsync();
            ShowDialog();
        }
        finally
        {
            _opening = false;
        }
    }
}
