using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoiceRepository
{
	int Add(Invoice invoice);

	bool RemoveById(int id);

	Invoice GetById(int id);

	IEnumerable<Invoice> GetByDateRange(DateOnly start, DateOnly end);

	IEnumerable<Invoice> GetByDate(DateOnly date);
}