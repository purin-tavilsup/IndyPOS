using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models.Report;

namespace IndyPOS.Windows.Forms.Interfaces
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

		PayLaterPaymentsReport GetArReport();
	}
}
