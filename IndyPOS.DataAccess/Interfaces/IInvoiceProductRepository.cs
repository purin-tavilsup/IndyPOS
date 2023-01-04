using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IInvoiceProductRepository
{
	int AddInvoiceProduct(InvoiceProduct product);

	IEnumerable<InvoiceProduct> GetInvoiceProductsByInvoiceId(int id);

	IEnumerable<InvoiceProduct> GetInvoiceProductsByDateRange(DateTime start, DateTime end);

	IEnumerable<InvoiceProduct> GetInvoiceProductsByDate(DateTime date);
}