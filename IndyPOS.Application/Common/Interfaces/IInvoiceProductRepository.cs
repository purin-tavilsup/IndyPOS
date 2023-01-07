using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoiceProductRepository
{
	int Add(InvoiceProduct product);

	IEnumerable<InvoiceProduct> GetByInvoiceId(int id);

	IEnumerable<InvoiceProduct> GetByDateRange(DateTime start, DateTime end);

	IEnumerable<InvoiceProduct> GetByDate(DateTime date);
}