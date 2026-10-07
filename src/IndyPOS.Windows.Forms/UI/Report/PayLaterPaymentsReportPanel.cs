using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.Common.Models;
using IndyPOS.Windows.Forms.UI;

namespace IndyPOS.Windows.Forms.UI.Report;

public partial class PayLaterPaymentsReportPanel : UserControl
{
    private const int CustomersPerPage = 200;

    private readonly IStoreHubClient _storeHubClient;
    private readonly MessageForm _messageForm;

    private enum AccountColumn
    {
        Description,
        ReceivableAmountTotal,
        RemainingAmountTotal
    }

    public PayLaterPaymentsReportPanel(IStoreHubClient storeHubClient, MessageForm messageForm)
    {
        _storeHubClient = storeHubClient;
        _messageForm = messageForm;

        InitializeComponent();
        InitializeUserDataView();
    }

    private void InitializeUserDataView()
    {
        #region Initialize all columns

        PayLaterPaymentsSummaryDataView.Columns.Clear();
        PayLaterPaymentsSummaryDataView.ColumnCount = 3;

        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.Description].Name = "คำอธิบาย";
        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.Description].Width = 250;
        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.Description].ReadOnly = true;

        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.ReceivableAmountTotal].Name = "ยอดลงบัญชีสะสม";
        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.ReceivableAmountTotal].Width = 250;
        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.ReceivableAmountTotal].ReadOnly = true;
		PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.ReceivableAmountTotal].DefaultCellStyle.Format = "N2";

        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.RemainingAmountTotal].Name = "ยอดค้างชำระสะสม";
        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.RemainingAmountTotal].Width = 250;
        PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.RemainingAmountTotal].ReadOnly = true;
		PayLaterPaymentsSummaryDataView.Columns[(int)AccountColumn.RemainingAmountTotal].DefaultCellStyle.Format = "N2";

        #endregion
    }

    private async Task ShowPaymentsByPeriodAsync(string periodText, TimePeriod period)
    {
        PeriodLabel.Text = periodText;

        try
        {
            var range = period.ToDateRange();
            ShowCustomers(await GetCustomersAsync(range.StartDate, range.EndDate));
        }
        catch (Exception ex)
        {
            ReportErrorHandler.Show(_messageForm, ex);
        }
    }

    private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
    {
        await ShowPaymentsByPeriodAsync(ShowReportByTodayButton.Text, TimePeriod.Today);
    }

    private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
    {
        await ShowPaymentsByPeriodAsync(ShowReportByThisMonthButton.Text, TimePeriod.ThisMonth);
    }

    private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
    {
        await ShowPaymentsByPeriodAsync(ShowReportByThisYearButton.Text, TimePeriod.ThisYear);
    }

	private async void ShowAllPayLaterPaymentsButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowAllPayLaterPaymentsButton.Text;

        try
        {
		    ShowCustomers(await GetCustomersAsync(null, null));
        }
        catch (Exception ex)
        {
            ReportErrorHandler.Show(_messageForm, ex);
        }
	}

    private async Task<IReadOnlyList<PayLaterSummaryDto>> GetCustomersAsync(DateOnly? fromDate, DateOnly? toDate) =>
        await PageReader.ReadAllAsync(async page =>
        {
            var report = await _storeHubClient.GetPayLaterReportAsync(fromDate, toDate, page, CustomersPerPage);
            return (report.Customers.Items, report.Customers.HasNextPage);
        });

    // Customers who still owe: each one's total ลงบัญชี and what is left to pay, as v3 showed them.
    private void ShowCustomers(IEnumerable<PayLaterSummaryDto> customers)
    {
        PayLaterPaymentsSummaryDataView.Rows.Clear();

        foreach (var customer in customers.Where(c => c.RemainingBalance > 0))
        {
            var row = new object[PayLaterPaymentsSummaryDataView.ColumnCount];
            row[(int)AccountColumn.Description] = customer.CustomerName;
            row[(int)AccountColumn.ReceivableAmountTotal] = customer.TotalOwed;
            row[(int)AccountColumn.RemainingAmountTotal] = customer.RemainingBalance;

            var rowIndex = PayLaterPaymentsSummaryDataView.Rows.Add(row);
            var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38, 38, 38) : Color.FromArgb(48, 48, 48);

            PayLaterPaymentsSummaryDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
        }
    }
}
