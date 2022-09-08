using IndyPOS.Facade.Models.Report;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IndyPOS.Facade.Interfaces
{
	public interface IReportHelper
	{
		IEnumerable<IFinalInvoice> Invoices { get; }

		IEnumerable<IFinalInvoiceProduct> InvoiceProducts { get; }

		IEnumerable<IFinalInvoicePayment> InvoicePayments { get; }

		SalesSummary CreateSalesSummary(IInvoiceInfo invoiceInfo);

		Task<SalesReport> UpdateReport(SalesSummary summary);

		Invoice CreateInvoiceForDataFeed(IInvoiceInfo invoiceInfo);
	}
}
