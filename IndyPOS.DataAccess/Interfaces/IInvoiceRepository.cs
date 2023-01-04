using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IInvoiceRepository
{
	int AddInvoice(Invoice invoice);

	Invoice? GetInvoiceByInvoiceId(int id);

	IEnumerable<Invoice> GetInvoicesByDateRange(DateTime start, DateTime end);

	IEnumerable<Invoice> GetInvoicesByDate(DateTime date);
}