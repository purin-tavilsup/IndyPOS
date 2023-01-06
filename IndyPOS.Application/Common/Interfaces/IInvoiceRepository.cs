using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoiceRepository
{
	int AddInvoice(Invoice invoice);

	Invoice? GetInvoiceByInvoiceId(int id);

	IEnumerable<Invoice> GetInvoicesByDateRange(DateTime start, DateTime end);

	IEnumerable<Invoice> GetInvoicesByDate(DateTime date);
}