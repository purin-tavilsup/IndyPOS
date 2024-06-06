using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Abstractions.Pos.Repositories;

public interface IInvoiceProductRepository
{
	int Add(InvoiceProduct product);

	bool RemoveById(int id);

	bool RemoveByInvoiceId(int id);

	IEnumerable<InvoiceProduct> GetByInvoiceId(int id);

	IEnumerable<InvoiceProduct> GetByDateRange(DateOnly start, DateOnly end);

	IEnumerable<InvoiceProduct> GetByDate(DateOnly date);
}