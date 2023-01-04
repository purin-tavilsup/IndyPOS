using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Models.Report;
using IndyPOS.Windows.Forms.Interfaces;

namespace IndyPOS.Windows.Forms.UI.Report
{
	public partial class SalesReportPanel : UserControl
    {
		private readonly IReportController _reportController;

		[ExcludeFromCodeCoverage]
        public SalesReportPanel(IReportController reportController)
        {
			_reportController = reportController;

			InitializeComponent();
        }
		
		private void ShowSummary(SalesSummary salesSummary, PaymentsSummary paymentsSummary, PayLaterPaymentsSummary arSummary)
		{
			OverallSaleLabel.Text = $"{salesSummary.InvoiceTotal:N}";

			OverallSaleExcluedIncompleteArLabel.Text = $"{salesSummary.InvoiceTotalWithoutAr:N}";
			
			GeneralGoodsSaleLabel.Text = $"{salesSummary.GeneralProductsTotal:N}";
			
			HardwareSaleLabel.Text = $"{salesSummary.HardwareProductsTotal:N}";

			ArTotalForGeneralProductsLabel.Text  = $"{salesSummary.ArTotalForGeneralProducts:N}";

			ArTotalForHardwareProductsLabel.Text = $"{salesSummary.ArTotalForHardwareProducts:N}";

			GeneralProductsTotalWithoutArLabel.Text = $"{salesSummary.GeneralProductsTotalWithoutAr:N}";

			HardwareProductsTotalWithoutArLabel.Text = $"{salesSummary.HardwareProductsTotalWithoutAr:N}";

			ArTotalLabel.Text = $"{arSummary.Total:N}";

            CompletedArLabel.Text = $"{arSummary.CompletedPaymentsTotal:N}";

			IncompleteArLabel.Text = $"{arSummary.IncompletePaymentsTotal:N}";

			PaymentByTransferLabel.Text = $"{paymentsSummary.MoneyTransferTotal:N}";

			PaymentByKlkLabel.Text = $"{paymentsSummary.FiftyFiftyTotal:N}";

			PaymentByM33Label.Text = $"{paymentsSummary.M33WeLoveTotal:N}";

			PaymentByWeWinLabel.Text = $"{paymentsSummary.WeWinTotal:N}";

			PaymentByWelfareCardLabel.Text = $"{paymentsSummary.WelfareCardTotal:N}";

			PaymentByArLabel.Text = $"{paymentsSummary.ArTotal:N}";
		}

		private async Task<SalesReport> GetSalesReportAsync()
		{
			return await _reportController.GetSaleReportAsync();
		}

		private async Task<PaymentsReport> GetPaymentsReportAsync()
		{
			return await _reportController.GetPaymentsReportAsync();
		}

		private PayLaterPaymentsReport GetArReport()
		{
			return _reportController.GetArReport();
		}

        private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
		{
			PeriodLabel.Text = ShowReportByTodayButton.Text;

			var salesReport = await GetSalesReportAsync();
			var paymentsReport = await GetPaymentsReportAsync();
			var arReport = GetArReport();

			ShowSummary(salesReport.DaySummary, paymentsReport.DaySummary, arReport.DaySummary);
		}

		private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisMonthButton.Text;

			var salesReport = await GetSalesReportAsync();
			var paymentsReport = await GetPaymentsReportAsync();
			var arReport = GetArReport();

			ShowSummary(salesReport.MonthSummary, paymentsReport.MonthSummary, arReport.MonthSummary);
        }

        private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisYearButton.Text;

			var salesReport = await GetSalesReportAsync();
			var paymentsReport = await GetPaymentsReportAsync();
			var arReport = GetArReport();

			ShowSummary(salesReport.YearSummary, paymentsReport.YearSummary, arReport.YearSummary);
        }

		private void TestDataFeedButton_Click(object sender, EventArgs e)
		{
		}
    }
}
