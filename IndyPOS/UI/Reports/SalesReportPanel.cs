using IndyPOS.Controllers;
using IndyPOS.Enums;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Windows.Media;

namespace IndyPOS.UI.Reports
{
    public partial class SalesReportPanel : UserControl
    {
		private readonly IReportController _reportController;
		private readonly MessageForm _messageForm;
		private readonly FontFamily _fontFamily;
		private const string FontFamilyName = "FC Subject [Non-commercial] Reg";

		[ExcludeFromCodeCoverage]
        public SalesReportPanel(IReportController reportController, MessageForm messageForm)
        {
			_reportController = reportController;
			_messageForm = messageForm;

			InitializeComponent();

			_fontFamily = new FontFamily(FontFamilyName);
        }
		
		private void ShowReport()
		{
			var invoicesTotal = _reportController.InvoicesTotal;

			var generalGoodsProductsTotal = _reportController.GeneralGoodsProductsTotal;

			var hardwareProductsTotal = _reportController.HardwareProductsTotal;

			var invoicesTotalWithoutAr = _reportController.InvoicesTotalWithoutAr;

			OverallSaleLabel.Text = $"{invoicesTotal:N}";

			OverallSaleExcluedArLabel.Text = $"{invoicesTotalWithoutAr:N}";
			
			GeneralGoodsSaleLabel.Text = $"{generalGoodsProductsTotal:N}";
			
			HardwareSaleLabel.Text = $"{hardwareProductsTotal:N}";

			PaymentByCashLabel.Text = $"{_reportController.GetPaymentsTotalByType(PaymentType.Cash):N}";

			PaymentByTransferLabel.Text = $"{_reportController.GetPaymentsTotalByType(PaymentType.MoneyTransfer):N}";

			PaymentByKlkLabel.Text = $"{_reportController.GetPaymentsTotalByType(PaymentType.FiftyFifty):N}";

			PaymentByM33Label.Text = $"{_reportController.GetPaymentsTotalByType(PaymentType.M33WeLove):N}";

			PaymentByWeWinLabel.Text = $"{_reportController.GetPaymentsTotalByType(PaymentType.WeWin):N}";

			PaymentByWelfareCardLabel.Text = $"{_reportController.GetPaymentsTotalByType(PaymentType.WelfareCard):N}";

			PaymentByArLabel.Text = $"{_reportController.GetPaymentsTotalByType(PaymentType.AccountReceivable):N}";
		}

        private void ShowReportByTodayButton_Click(object sender, EventArgs e)
		{
			_reportController.LoadInvoicesByPeriod(ReportPeriod.Today);

			ShowReport();
		}

        private void ShowReportByThisWeekButton_Click(object sender, EventArgs e)
        {
			_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisWeek);

			ShowReport();
        }

        private void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
        {
			_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisMonth);

			ShowReport();
        }

        private void ShowReportByThisYearButton_Click(object sender, EventArgs e)
        {
			//_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisYear);

			//ShowReport();
        }

        private void ShowReportByDateRangeButton_Click(object sender, EventArgs e)
		{
			var startDate = StartDatePicker.Value;
			var endDate = EndDatePicker.Value;

			_reportController.LoadInvoicesByDateRange(startDate, endDate);

			ShowReport();
		}

        private void WriteSaleRecordsToFileButton_Click(object sender, EventArgs e)
		{
			var date = SaveSaleRecordsDateTimePicker.Value;

			_reportController.WriteSaleRecordsToCsvFileByDate(date);

			_messageForm.Show($"รายการขายในวันที่ {date:yyyy-MM-dd} ถูกบันทึกลงไฟล์เรียบร้อยแล้ว", " บันทึกรายการขายลงไฟล์");
        }
    }
}
