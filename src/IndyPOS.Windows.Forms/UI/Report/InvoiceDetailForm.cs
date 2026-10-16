using IndyPOS.Application.Common.Models;
using IndyPOS.Presentation.SalesHistory;
using IndyPOS.Windows.Forms.Services;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>
/// One bill in a dialog (spec §7: the old SaleHistoryByInvoiceIdForm), opened by the bill's Guid, e.g. from a
/// ลงบัญชี debt. Each open gets a fresh ViewModel for whoever is logged in now.
/// </summary>
public sealed class InvoiceDetailForm : Form
{
    private readonly SalesHistoryViewModelFactory _factory;
    private readonly IStoreFeaturesProvider _features;
    private readonly InvoiceDetailView _view = new() { Dock = DockStyle.Fill };

    public InvoiceDetailForm(SalesHistoryViewModelFactory factory, IStoreFeaturesProvider features)
    {
        _factory = factory;
        _features = features;

        Name = nameof(InvoiceDetailForm);
        Text = "รายละเอียดบิล";
        Size = new Size(1000, 850);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(38, 38, 38);
        Controls.Add(_view);
    }

    /// <summary>Binds a fresh ViewModel and loads the bill, without showing the dialog.</summary>
    public async Task LoadAsync(Guid saleId)
    {
        var viewModel = _factory.CreateDetail();
        _view.ShowProductTypeSplit = (await LayoutAsync()).ShowProductTypeSplit;
        _view.Bind(viewModel);

        await viewModel.LoadAsync(saleId);
    }

    public async Task ShowBillAsync(Guid saleId)
    {
        await LoadAsync(saleId);
        ShowDialog();
    }

    private async Task<TillLayout> LayoutAsync()
    {
        try
        {
            return TillLayout.For(await _features.GetAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for the bill detail");
            return TillLayout.WhenFeaturesUnavailable;
        }
    }
}
