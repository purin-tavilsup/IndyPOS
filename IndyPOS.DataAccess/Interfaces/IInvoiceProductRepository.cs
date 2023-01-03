using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces
{
    public interface IInvoiceProductRepository
    {
		int AddInvoiceProduct(InvoiceProduct product);

		IList<InvoiceProduct> GetInvoiceProductsByInvoiceId(int id);

		IList<InvoiceProduct> GetInvoiceProductsByDateRange(DateTime start, DateTime end);

		IList<InvoiceProduct> GetInvoiceProductsByDate(DateTime date);
    }
}
