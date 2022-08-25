using IndyPOS.Controllers;
using IndyPOS.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndyPOS.DataAccess.Models;
using IndyPOS.Sales;
using PaymentType = IndyPOS.DataAccess.Models.PaymentType;

namespace IndyPOS.UI.Reports
{
    public partial class SalesReportPanel : UserControl
    {
		private readonly IReportController _reportController;
		private readonly MessageForm _messageForm;

		[ExcludeFromCodeCoverage]
        public SalesReportPanel(IReportController reportController, MessageForm messageForm)
        {
			_reportController = reportController;
			_messageForm = messageForm;

			InitializeComponent();
        }
		
		private void ShowReport(SalesReport saleReport, PaymentReport paymentReport, ArReport arReport)
		{
			OverallSaleLabel.Text = $"{saleReport.InvoicesTotal:N}";

			OverallSaleExcluedIncompleteArLabel.Text = $"{saleReport.InvoicesTotalWithoutAr:N}";
			
			GeneralGoodsSaleLabel.Text = $"{saleReport.GeneralProductsTotal:N}";
			
			HardwareSaleLabel.Text = $"{saleReport.HardwareProductsTotal:N}";

			ArTotalForGeneralProductsLabel.Text  = $"{saleReport.ArTotalForGeneralProducts:N}";

			ArTotalForHardwareProductsLabel.Text = $"{saleReport.ArTotalForHardwareProducts:N}";

			GeneralProductsTotalWithoutArLabel.Text = $"{saleReport.GeneralProductsTotalWithoutAr:N}";

			HardwareProductsTotalWithoutArLabel.Text = $"{saleReport.HardwareProductsTotalWithoutAr:N}";

			ArTotalLabel.Text = $"{arReport.ArTotal:N}";

            CompletedArLabel.Text = $"{arReport.CompletedArTotal:N}";

			IncompleteArLabel.Text = $"{arReport.IncompleteArTotal:N}";

			PaymentByTransferLabel.Text = $"{paymentReport.MoneyTransferTotal:N}";

			PaymentByKlkLabel.Text = $"{paymentReport.FiftyFiftyTotal:N}";

			PaymentByM33Label.Text = $"{paymentReport.M33WeLoveTotal:N}";

			PaymentByWeWinLabel.Text = $"{paymentReport.WeWinTotal:N}";

			PaymentByWelfareCardLabel.Text = $"{paymentReport.WelfareCardTotal:N}";

			PaymentByArLabel.Text = $"{paymentReport.ArTotal:N}";
		}

		private async Task<SalesReport> GetSaleReportAsync()
		{
			return await Task.Run(() => _reportController.GetSaleReport());
		}

		private async Task<PaymentReport> GetPaymentReportAsync()
		{
			return await Task.Run(() => _reportController.GetPaymentReport());
		}

		private async Task<ArReport> GetArReportAsync()
		{
			return await Task.Run(() => _reportController.GetArReport());
		}

        private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
		{
			PeriodLabel.Text = ShowReportByTodayButton.Text;

			_reportController.LoadInvoicesByPeriod(ReportPeriod.Today);

			var getSaleReportTask = Task.Run(GetSaleReportAsync);
			var getPaymentReportTask = Task.Run(GetPaymentReportAsync);
			var getArReportTask = Task.Run(GetArReportAsync);

			var tasks = new List<Task>
			{
				getSaleReportTask,
				getPaymentReportTask,
				getArReportTask
			};

			await Task.WhenAll(tasks.ToArray());

			var saleReport = await getSaleReportTask;
			var paymentReport = await getPaymentReportTask;
			var arReport = await getArReportTask;

			ShowReport(saleReport, paymentReport, arReport);
		}

        private async void ShowReportByThisWeekButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisWeekButton.Text;

			_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisWeek);

			var getSaleReportTask = Task.Run(GetSaleReportAsync);
			var getPaymentReportTask = Task.Run(GetPaymentReportAsync);
			var getArReportTask = Task.Run(GetArReportAsync);

			var tasks = new List<Task>
			{
				getSaleReportTask,
				getPaymentReportTask,
				getArReportTask
			};

			await Task.WhenAll(tasks.ToArray());

			var saleReport = await getSaleReportTask;
			var paymentReport = await getPaymentReportTask;
			var arReport = await getArReportTask;

			ShowReport(saleReport, paymentReport, arReport);
        }

        private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisMonthButton.Text;

			_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisMonth);

			var getSaleReportTask = Task.Run(GetSaleReportAsync);
			var getPaymentReportTask = Task.Run(GetPaymentReportAsync);
			var getArReportTask = Task.Run(GetArReportAsync);

			var tasks = new List<Task>
			{
				getSaleReportTask,
				getPaymentReportTask,
				getArReportTask
			};

			await Task.WhenAll(tasks.ToArray());

			var saleReport = await getSaleReportTask;
			var paymentReport = await getPaymentReportTask;
			var arReport = await getArReportTask;

			ShowReport(saleReport, paymentReport, arReport);
        }

        private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
        {
			PeriodLabel.Text = ShowReportByThisYearButton.Text;

            _reportController.LoadInvoicesByPeriod(ReportPeriod.ThisYear);

			var getSaleReportTask = Task.Run(GetSaleReportAsync);
			var getPaymentReportTask = Task.Run(GetPaymentReportAsync);
			var getArReportTask = Task.Run(GetArReportAsync);

			var tasks = new List<Task>
			{
				getSaleReportTask,
				getPaymentReportTask,
				getArReportTask
			};

			await Task.WhenAll(tasks.ToArray());

			var saleReport = await getSaleReportTask;
			var paymentReport = await getPaymentReportTask;
			var arReport = await getArReportTask;

			ShowReport(saleReport, paymentReport, arReport);
        }

        private async void ShowReportByDateRangeButton_Click(object sender, EventArgs e)
		{
			var startDate = StartDatePicker.Value;
			var endDate = EndDatePicker.Value;

			PeriodLabel.Text = $"{startDate:yyyy MMMM dd} - {endDate:yyyy MMMM dd}";

			_reportController.LoadInvoicesByDateRange(startDate, endDate);

			var getSaleReportTask = Task.Run(GetSaleReportAsync);
			var getPaymentReportTask = Task.Run(GetPaymentReportAsync);
			var getArReportTask = Task.Run(GetArReportAsync);

			var tasks = new List<Task>
			{
				getSaleReportTask,
				getPaymentReportTask,
				getArReportTask
			};

			await Task.WhenAll(tasks.ToArray());

			var saleReport = await getSaleReportTask;
			var paymentReport = await getPaymentReportTask;
			var arReport = await getArReportTask;

			ShowReport(saleReport, paymentReport, arReport);
		}

        private void TestDataFeedButton_Click(object sender, EventArgs e)
		{
		}
    }
}
