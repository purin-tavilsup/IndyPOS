using IndyPOS.Facade.Models;

namespace IndyPOS.Interfaces
{
	public interface ISaleInvoiceMapper
	{
		Invoice Map(ISaleInvoice invoice);
	}
}
