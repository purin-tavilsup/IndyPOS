using IndyPOS.Common.Enums;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;

namespace IndyPOS.Interfaces
{
	public interface IReportController
	{
		IEnumerable<IFinalInvoice> GetInvoicesByPeriod(TimePeriod period);

		IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId);

		Task<SalesReport> GetSaleReportAsync();

		Task<PaymentsReport> GetPaymentsReportAsync();

		ArReport GetArReport();
	}
}
