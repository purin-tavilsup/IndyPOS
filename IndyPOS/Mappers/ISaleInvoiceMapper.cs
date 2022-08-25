using IndyPOS.Facade.Models;
using IndyPOS.Sales;

namespace IndyPOS.Mappers
{
	public interface ISaleInvoiceMapper
	{
		Invoice Map(ISaleInvoice invoice);
	}
}
