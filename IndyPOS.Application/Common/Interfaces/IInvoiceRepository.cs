using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoiceRepository
{
	int Add(Invoice invoice);

	Invoice? GetById(int id);

	IEnumerable<Invoice> GetByDateRange(DateTime start, DateTime end);

	IEnumerable<Invoice> GetByDate(DateTime date);
}