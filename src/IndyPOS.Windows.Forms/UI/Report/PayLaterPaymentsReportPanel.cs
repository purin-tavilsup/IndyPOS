using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.PayLaterPayments;

namespace IndyPOS.Windows.Forms.UI.Report;

public partial class PayLaterPaymentsReportPanel : UserControl
{
    private readonly IReportService _reportService;

    private enum AccountColumn
    {
        Description,
        ReceivableAmountTotal,
        RemainingAmountTotal
    }

    public PayLaterPaymentsReportPanel(IReportService reportService)
    {
        _reportService = reportService;

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

    private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
    {
        PeriodLabel.Text = ShowReportByTodayButton.Text;

        var payments = await _reportService.GetPayLaterPaymentsByPeriodAsync(TimePeriod.Today);

        ShowPayments(payments);
    }

    private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
    {
        PeriodLabel.Text = ShowReportByThisMonthButton.Text;

        var payments = await _reportService.GetPayLaterPaymentsByPeriodAsync(TimePeriod.ThisMonth);

        ShowPayments(payments);
    }

    private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
    {
        PeriodLabel.Text = ShowReportByThisYearButton.Text;

        var payments = await _reportService.GetPayLaterPaymentsByPeriodAsync(TimePeriod.ThisYear);

        ShowPayments(payments);
    }

	private async void ShowAllPayLaterPaymentsButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowAllPayLaterPaymentsButton.Text;

		var payments = await _reportService.GetPayLaterPaymentsAsync();

		ShowPayments(payments);
	}

    private void ShowPayments(IEnumerable<PayLaterPaymentDto> payments)
    {
        PayLaterPaymentsSummaryDataView.Rows.Clear();

        var groups = payments.GroupBy(x => x.Description)
							 .Where(g => g.Sum(p => p.ReceivableAmount - p.PaidAmount) > 0);

        foreach (var group in groups)
        {
            AddToPayLaterPaymentsSummaryDataView(group);
        }
    }

    private void AddToPayLaterPaymentsSummaryDataView(IGrouping<string, PayLaterPaymentDto> paymentGroup)
    {
        var description = paymentGroup.Key;
        var receivableAmountTotal = paymentGroup.Sum(x => x.ReceivableAmount);
        var remainingAmountTotal = paymentGroup.Sum(x => x.ReceivableAmount - x.PaidAmount);

        var columnCount = PayLaterPaymentsSummaryDataView.ColumnCount;
        var row = new object[columnCount];

        row[(int)AccountColumn.Description] = description;
		row[(int) AccountColumn.ReceivableAmountTotal] = receivableAmountTotal;
        row[(int)AccountColumn.RemainingAmountTotal] = remainingAmountTotal;

        var rowIndex = PayLaterPaymentsSummaryDataView.Rows.Add(row);
        var rowBackColor = rowIndex % 2 == 0 ? Color.FromArgb(38, 38, 38) : Color.FromArgb(48, 48, 48);

        PayLaterPaymentsSummaryDataView.Rows[rowIndex].DefaultCellStyle.BackColor = rowBackColor;
    }
}