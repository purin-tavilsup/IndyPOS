using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using IndyPOS.Controllers;
using IndyPOS.Enums;
using LiveCharts;
using LiveCharts.Wpf;

namespace IndyPOS.UI.Reports
{
    public partial class SalesReportPanel : UserControl
    {
		private readonly IReportController _reportController;
		private readonly FontFamily _fontFamily;
		private const string FontFamilyName = "FC Subject [Non-commercial] Reg";

		[ExcludeFromCodeCoverage]
        public SalesReportPanel(IReportController reportController)
        {
			_reportController = reportController;

			InitializeComponent();

			_fontFamily = new FontFamily(FontFamilyName);
        }
		
        private void ShowReportByTodayButton_Click(object sender, EventArgs e)
		{
			_reportController.LoadInvoicesByPeriod(ReportPeriod.Today);

			var invoicesTotal = _reportController.InvoicesTotal;

			var generalGoodsProductsTotal = _reportController.GeneralGoodsProductsTotal;

			var hardwareProductsTotal = _reportController.HardwareProductsTotal;

			OverallSaleLabel.Text = $"{invoicesTotal:N}";
			GeneralGoodsSaleLabel.Text = $"{generalGoodsProductsTotal:N}";
			HardwareSaleLabel.Text = $"{hardwareProductsTotal:N}";
		}
    }
}
