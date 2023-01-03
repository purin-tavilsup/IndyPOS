using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IInvoiceRepository
{
	int AddInvoice(Invoice invoice);

	Invoice GetInvoiceByInvoiceId(int id);

	IList<Invoice> GetInvoicesByDateRange(DateTime start, DateTime end);

	IList<Invoice> GetInvoicesByDate(DateTime date);
}