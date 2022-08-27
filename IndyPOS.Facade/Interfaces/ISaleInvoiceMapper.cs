using IndyPOS.Facade.Models;

namespace IndyPOS.Facade.Interfaces
{
	public interface ISaleInvoiceMapper
	{
		Invoice Map(ISaleInvoice invoice);
	}
}
