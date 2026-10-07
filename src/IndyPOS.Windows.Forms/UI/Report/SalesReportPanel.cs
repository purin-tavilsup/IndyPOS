using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI;
using Serilog;

namespace IndyPOS.Windows.Forms.UI.Report;

public partial class SalesReportPanel : UserControl
{
    private readonly IReportService _reportService;
    private readonly IStoreHubClient _storeHubClient;
    private readonly IStoreFeaturesProvider _storeFeatures;
    private readonly MessageForm _messageForm;

    [ExcludeFromCodeCoverage]
    public SalesReportPanel(IReportService reportService,
                            IStoreHubClient storeHubClient,
                            IStoreFeaturesProvider storeFeatures,
                            MessageForm messageForm)
    {
        _reportService = reportService;
        _storeHubClient = storeHubClient;
        _storeFeatures = storeFeatures;
        _messageForm = messageForm;

        InitializeComponent();

        // Initialize date pickers to today
        StartDatePicker.Value = DateTime.Today;
        EndDatePicker.Value = DateTime.Today;
    }

    // Which tiles need which store features. The others ("ยอดขาย : ทั้งหมด") show for every store.
    private Control[] LedgerTiles => [panel14, panel21, panel17, panel18];
    private Control[] ProductTypeSplitTiles => [panel4, panel5];
    private Control[] SplitLedgerTiles => [panel20, panel22, panel19, panel16];

    private async Task ApplyStoreLayoutAsync()
    {
        TillLayout layout;
        try
        {
            layout = TillLayout.For(await _storeFeatures.GetAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not load store features for the sales overview");
            layout = TillLayout.WhenFeaturesUnavailable;
        }

        foreach (var tile in LedgerTiles) tile.Visible = layout.ShowPayLaterReports;
        foreach (var tile in ProductTypeSplitTiles) tile.Visible = layout.ShowProductTypeSplit;
        foreach (var tile in SplitLedgerTiles) tile.Visible = layout.ShowPayLaterReports && layout.ShowProductTypeSplit;
    }

    private void ShowSummary(SalesSummary salesSummary, SalesSummaryDto summary)
    {
        OverallSaleLabel.Text = $"{salesSummary.InvoiceTotal:N2}";

        OverallSaleExcluedIncompleteArLabel.Text = $"{salesSummary.InvoiceTotalWithoutPayLaterPayments:N2}";

        GeneralGoodsSaleLabel.Text = $"{salesSummary.GeneralProductsTotal:N2}";

        HardwareSaleLabel.Text = $"{salesSummary.HardwareProductsTotal:N2}";

        ArTotalForGeneralProductsLabel.Text = $"{salesSummary.PayLaterPaymentsTotalForGeneralProducts:N2}";

        ArTotalForHardwareProductsLabel.Text = $"{salesSummary.PayLaterPaymentsTotalForHardwareProducts:N2}";

        GeneralProductsTotalWithoutArLabel.Text = $"{salesSummary.GeneralProductsTotalWithoutPayLaterPayments:N2}";

        HardwareProductsTotalWithoutArLabel.Text = $"{salesSummary.HardwareProductsTotalWithoutPayLaterPayments:N2}";

        ArTotalLabel.Text = $"{salesSummary.PayLaterPaymentsTotal:N2}";

        CompletedArLabel.Text = $"{salesSummary.CompletedPayLaterPaymentsTotal:N2}";

        IncompleteArLabel.Text = $"{salesSummary.IncompletePayLaterPaymentsTotal:N2}";

        MoneyRowsPanel.SuspendLayout();
        MoneyRowsPanel.Controls.Clear();
        foreach (var row in MoneyRows.From(summary))
            MoneyRowsPanel.Controls.Add(MoneyRowView.Create(row));
        MoneyRowsPanel.ResumeLayout();
    }

    private async Task ShowReportByPeriodAsync(string periodText, TimePeriod period)
    {
        var range = period.ToDateRange();
        await ShowReportAsync(periodText, range.StartDate, range.EndDate);
    }

    private async Task ShowReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        await ShowReportAsync($"{startDate:yyyy MMMM dd} - {endDate:yyyy MMMM dd}", startDate, endDate);
    }

    private async Task ShowReportAsync(string periodText, DateOnly startDate, DateOnly endDate)
    {
        PeriodLabel.Text = periodText;

        try
        {
            await ApplyStoreLayoutAsync();

            var salesReport = await _reportService.CreateSalesSummaryByDateRangeAsync(startDate, endDate);
            var summary = await _storeHubClient.GetSalesSummaryAsync(startDate, endDate);

            ShowSummary(salesReport, summary);
        }
        catch (Exception ex)
        {
            ReportErrorHandler.Show(_messageForm, ex);
        }
    }

    private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
    {
        await ShowReportByPeriodAsync(ShowReportByTodayButton.Text, TimePeriod.Today);
    }

    private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
    {
        await ShowReportByPeriodAsync(ShowReportByThisMonthButton.Text, TimePeriod.ThisMonth);
    }

    private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
    {
        await ShowReportByPeriodAsync(ShowReportByThisYearButton.Text, TimePeriod.ThisYear);
    }

    private void TestDataFeedButton_Click(object sender, EventArgs e)
    {
    }

    private async void ShowReportByDateRangeButton_Click(object sender, EventArgs e)
    {
		var startDate = StartDatePicker.Value.ToDateOnly();
		var endDate = EndDatePicker.Value.ToDateOnly();

        await ShowReportByDateRangeAsync(startDate, endDate);
	}
}
