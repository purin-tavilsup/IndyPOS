using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models.Report;
using IndyPOS.Windows.Forms.Interfaces;

namespace IndyPOS.Windows.Forms.Controllers
{
    public class ReportController : IReportController
	{
		private readonly IReportHelper _reportHelper;

		public ReportController(IReportHelper reportHelper)
        {
			_reportHelper = reportHelper;
        }

		public async Task<SalesReport> GetSaleReportAsync()
		{
			return await _reportHelper.GetSalesReportAsync();
		}

		public async Task<PaymentsReport> GetPaymentsReportAsync()
		{
			return await _reportHelper.GetPaymentsReportAsync();
		}

		public PayLaterPaymentsReport GetArReport()
		{
			return _reportHelper.GetArReport();
		}

		public IEnumerable<IFinalInvoice> GetInvoicesByPeriod(TimePeriod period)
		{
			return _reportHelper.GetInvoicesByPeriod(period);
		}

		public IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateTime startDate, DateTime endDate)
		{
			return _reportHelper.GetInvoicesByDateRange(startDate, endDate);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date)
		{
			return _reportHelper.GetInvoiceProductsByDate(date);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateTime startDate, DateTime endDate)
		{
			return _reportHelper.GetInvoiceProductsByDateRange(startDate, endDate);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId)
		{
			return _reportHelper.GetInvoiceProductsByInvoiceId(invoiceId);
		}

		public IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId)
		{
			return _reportHelper.GetPaymentsByInvoiceId(invoiceId);
		}
	}
}
