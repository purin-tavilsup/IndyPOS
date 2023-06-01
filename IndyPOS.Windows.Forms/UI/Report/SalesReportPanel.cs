using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Report;

public partial class SalesReportPanel : UserControl
{
	private readonly IReportService _reportService;

	[ExcludeFromCodeCoverage]
	public SalesReportPanel(IReportService reportService)
	{
		_reportService = reportService;

		InitializeComponent();
	}

	private void ShowSummary(ISalesReport salesReport, IPaymentsReport paymentsReport)
	{
		OverallSaleLabel.Text = $"{salesReport.InvoiceTotal:N}";

		OverallSaleExcluedIncompleteArLabel.Text = $"{salesReport.InvoiceTotalWithoutPayLaterPayments:N}";
			
		GeneralGoodsSaleLabel.Text = $"{salesReport.GeneralProductsTotal:N}";
			
		HardwareSaleLabel.Text = $"{salesReport.HardwareProductsTotal:N}";

		ArTotalForGeneralProductsLabel.Text  = $"{salesReport.PayLaterPaymentsTotalForGeneralProducts:N}";

		ArTotalForHardwareProductsLabel.Text = $"{salesReport.PayLaterPaymentsTotalForHardwareProducts:N}";

		GeneralProductsTotalWithoutArLabel.Text = $"{salesReport.GeneralProductsTotalWithoutPayLaterPayments:N}";

		HardwareProductsTotalWithoutArLabel.Text = $"{salesReport.HardwareProductsTotalWithoutPayLaterPayments:N}";

		ArTotalLabel.Text = $"{salesReport.PayLaterPaymentsTotal:N}";

		CompletedArLabel.Text = $"{salesReport.CompletedPayLaterPaymentsTotal:N}";

		IncompleteArLabel.Text = $"{salesReport.IncompletePayLaterPaymentsTotal:N}";

		PaymentByTransferLabel.Text = $"{paymentsReport.MoneyTransferTotal:N}";

		PaymentByKlkLabel.Text = $"{paymentsReport.FiftyFiftyTotal:N}";

		PaymentByM33Label.Text = $"{paymentsReport.M33WeLoveTotal:N}";

		PaymentByWeWinLabel.Text = $"{paymentsReport.WeWinTotal:N}";

		PaymentByWelfareCardLabel.Text = $"{paymentsReport.WelfareCardTotal:N}";

		PaymentByArLabel.Text = $"{paymentsReport.PayLaterTotal:N}";
	}

	private async Task<ISalesReport> GetSalesReportByPeriodAsync(TimePeriod period)
	{
		return await _reportService.CreateSalesReportByPeriodAsync(period);
	}

	private async Task<IPaymentsReport> GetPaymentsReportAsync(TimePeriod period)
	{
		return await _reportService.CreatePaymentsReportByPeriodAsync(period);
	}

	private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowReportByTodayButton.Text;

		var salesReport = await GetSalesReportByPeriodAsync(TimePeriod.Today);
		var paymentsReport = await GetPaymentsReportAsync(TimePeriod.Today);

		ShowSummary(salesReport, paymentsReport);
	}

	private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowReportByThisMonthButton.Text;

		var salesReport = await GetSalesReportByPeriodAsync(TimePeriod.ThisMonth);
		var paymentsReport = await GetPaymentsReportAsync(TimePeriod.ThisMonth);

		ShowSummary(salesReport, paymentsReport);
	}

	private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
	{
		PeriodLabel.Text = ShowReportByThisYearButton.Text;

		var salesReport = await GetSalesReportByPeriodAsync(TimePeriod.ThisYear);
		var paymentsReport = await GetPaymentsReportAsync(TimePeriod.ThisYear);

		ShowSummary(salesReport, paymentsReport);
	}

	private void TestDataFeedButton_Click(object sender, EventArgs e)
	{
	}
}