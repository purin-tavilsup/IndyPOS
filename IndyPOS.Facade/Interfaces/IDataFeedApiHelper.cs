using IndyPOS.Facade.Models;
using System.Threading.Tasks;

namespace IndyPOS.Facade.Interfaces
{
    public interface IDataFeedApiHelper
	{
		Task PushInvoice(Invoice invoice);

		Task PushReport(SalesReport report);
	}
}
