using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// Reports → ประวัติการขาย (spec §7). Every visit gets a fresh list for whoever is logged in now,
/// opened on today.
/// </summary>
public sealed class SalesHistoryReportPanel : UserControl
{
    private readonly SalesHistoryViewModelFactory _factory;
    private readonly IStoreFeaturesProvider _features;
    private readonly InvoiceHistoryView _view = new() { Dock = DockStyle.Fill };

    public SalesHistoryReportPanel(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)
    {
        _factory = factory;
        _features = features;

        Name = nameof(SalesHistoryReportPanel);
        BackColor = TillStyle.Background;
        Controls.Add(_view);
        VisibleChanged += OnVisibleChanged;
    }

    public async Task OpenAsync()
    {
        InvoiceHistoryViewModel viewModel;
        try
        {
            viewModel = _factory.CreateHistory();
        }
        catch (InvalidOperationException ex)
        {
            // Only reachable through a stale event after a logout; the screen stays empty.
            Log.Warning(ex, "Sales history opened with nobody logged in");
            return;
        }

        _view.ShowProductTypeSplit = (await _features.GetLayoutAsync("sales history")).ShowProductTypeSplit;
        _view.Bind(viewModel);

        await viewModel.ShowTodayCommand.ExecuteAsync(null);
    }

    private async void OnVisibleChanged(object? sender, EventArgs e)
    {
        if (Visible)
            await OpenAsync();
    }
}
