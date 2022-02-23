using IndyPOS.Controllers;
using IndyPOS.Enums;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndyPOS.CloudReport;
using IndyPOS.Sales;

namespace IndyPOS.UI.Reports
{
    public partial class SalesReportPanel : UserControl
    {
		private readonly IReportController _reportController;
		private readonly MessageForm _messageForm;
		private readonly ICloudReportHelper _cloudReportHelper;

		[ExcludeFromCodeCoverage]
        public SalesReportPanel(IReportController reportController, MessageForm messageForm, ICloudReportHelper cloudReportHelper)
        {
			_reportController = reportController;
			_messageForm = messageForm;
			_cloudReportHelper = cloudReportHelper;

			InitializeComponent();
        }
		
		private void ShowReport(SaleReport saleReport, PaymentReport paymentReport, ArReport arReport)
		{
			OverallSaleLabel.Text = $"{saleReport.InvoicesTotal:N}";

			OverallSaleExcluedIncompleteArLabel.Text = $"{saleReport.InvoicesTotalWithoutAr:N}";
			
			GeneralGoodsSaleLabel.Text = $"{saleReport.GeneralProductsTotal:N}";
			
			HardwareSaleLabel.Text = $"{saleReport.HardwareProductsTotal:N}";

			GeneralProductsTotalWithoutArLabel.Text = $"{saleReport.GeneralProductsTotalWithoutAr:N}";

			HardwareProductsTotalWithoutArLabel.Text = $"{saleReport.HardwareProductsTotalWithoutAr:N}";

			ArTotalLabel.Text = $"{arReport.ArTotal:N}";

            CompletedArLabel.Text = $"{arReport.CompletedArTotal:N}";

			IncompleteArLabel.Text = $"{arReport.IncompleteArTotal:N}";

			PaymentByCashLabel.Text = $"{paymentReport.CashTotal:N}";

			PaymentByTransferLabel.Text = $"{paymentReport.MoneyTransferTotal:N}";

			PaymentByKlkLabel.Text = $"{paymentReport.FiftyFiftyTotal:N}";

			PaymentByM33Label.Text = $"{paymentReport.M33WeLoveTotal:N}";

			PaymentByWeWinLabel.Text = $"{paymentReport.WeWinTotal:N}";

			PaymentByWelfareCardLabel.Text = $"{paymentReport.WelfareCardTotal:N}";

			PaymentByArLabel.Text = $"{paymentReport.ArTotal:N}";
		}

		private async Task<SaleReport> GetSaleReportAsync()
		{
			return await Task.Run(_reportController.CreateSaleReport);
		}

		private async Task<PaymentReport> GetPaymentReportAsync()
		{
			return await Task.Run(_reportController.CreatePaymentReport);
		}

		private async Task<ArReport> GetArReportAsync()
		{
			return await Task.Run(_reportController.CreateArReport);
		}

        private void ShowReportByTodayButton_Click(object sender, EventArgs e)
		{
			PeriodLabel.Text = ShowReportByTodayButton.Text;

			_reportController.LoadInvoicesByPeriod(ReportPeriod.Today);

			var saleReport = Task.Run(GetSaleReportAsync).GetAwaiter().GetResult();
			var paymentReport = Task.Run(GetPaymentReportAsync).GetAwaiter().GetResult();
			var arReport = Task.Run(GetArReportAsync).GetAwaiter().GetResult();

			ShowReport(saleReport, paymentReport, arReport);
		}

        private void ShowReportByThisWeekButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisWeekButton.Text;

			_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisWeek);

			var saleReport = Task.Run(GetSaleReportAsync).GetAwaiter().GetResult();
			var paymentReport = Task.Run(GetPaymentReportAsync).GetAwaiter().GetResult();
			var arReport = Task.Run(GetArReportAsync).GetAwaiter().GetResult();

			ShowReport(saleReport, paymentReport, arReport);
        }

        private void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisMonthButton.Text;

			_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisMonth);

			var saleReport = Task.Run(GetSaleReportAsync).GetAwaiter().GetResult();
			var paymentReport = Task.Run(GetPaymentReportAsync).GetAwaiter().GetResult();
			var arReport = Task.Run(GetArReportAsync).GetAwaiter().GetResult();

			ShowReport(saleReport, paymentReport, arReport);
        }

        private void ShowReportByThisYearButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisYearButton.Text;

            _reportController.LoadInvoicesByPeriod(ReportPeriod.ThisYear);

			var saleReport = Task.Run(GetSaleReportAsync).GetAwaiter().GetResult();
			var paymentReport = Task.Run(GetPaymentReportAsync).GetAwaiter().GetResult();
			var arReport = Task.Run(GetArReportAsync).GetAwaiter().GetResult();

			ShowReport(saleReport, paymentReport, arReport);
        }

        private void ShowReportByDateRangeButton_Click(object sender, EventArgs e)
		{
			var startDate = StartDatePicker.Value;
			var endDate = EndDatePicker.Value;

			PeriodLabel.Text = $"{startDate:yyyy MMMM dd} - {endDate:yyyy MMMM dd}";

			_reportController.LoadInvoicesByDateRange(startDate, endDate);

			var saleReport = Task.Run(GetSaleReportAsync).GetAwaiter().GetResult();
			var paymentReport = Task.Run(GetPaymentReportAsync).GetAwaiter().GetResult();
			var arReport = Task.Run(GetArReportAsync).GetAwaiter().GetResult();

			ShowReport(saleReport, paymentReport, arReport);
		}

        private void WriteSaleRecordsToFileButton_Click(object sender, EventArgs e)
		{
			Task.Run(() => _cloudReportHelper.PublishSaleReport(2878)).GetAwaiter();
		}
    }
}
