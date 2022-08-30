using IndyPOS.Facade.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using IndyPOS.Facade.Models.Report;

namespace IndyPOS.Facade.Interfaces
{
    public interface IReportHelper
	{
		IEnumerable<IFinalInvoice> Invoices { get; }

		IEnumerable<IFinalInvoiceProduct> InvoiceProducts { get; }

		IEnumerable<IFinalInvoicePayment> InvoicePayments { get; }

		Task<SalesReport> UpdateReport(SalesSummary summary);
	}
}
