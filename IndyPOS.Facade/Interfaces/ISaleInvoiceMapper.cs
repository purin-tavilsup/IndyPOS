using IndyPOS.Facade.Models;
using IndyPOS.Facade.Models.Report;

namespace IndyPOS.Facade.Interfaces
{
	public interface ISaleInvoiceMapper
	{
		Invoice Map(ISaleInvoice invoice);
	}
}
