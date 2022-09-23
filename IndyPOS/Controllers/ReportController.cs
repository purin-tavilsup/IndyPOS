using IndyPOS.Common.Enums;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using IndyPOS.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IndyPOS.Controllers
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

		public ArReport GetArReport()
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
