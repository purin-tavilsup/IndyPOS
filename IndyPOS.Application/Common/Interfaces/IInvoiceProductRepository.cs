using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoiceProductRepository
{
	int AddInvoiceProduct(InvoiceProduct product);

	IEnumerable<InvoiceProduct> GetInvoiceProductsByInvoiceId(int id);

	IEnumerable<InvoiceProduct> GetInvoiceProductsByDateRange(DateTime start, DateTime end);

	IEnumerable<InvoiceProduct> GetInvoiceProductsByDate(DateTime date);
}