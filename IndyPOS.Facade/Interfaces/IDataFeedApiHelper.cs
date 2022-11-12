using IndyPOS.Facade.Models.Report;

namespace IndyPOS.Facade.Interfaces
{
	public interface IDataFeedApiHelper
	{
		Task PushInvoice(Invoice invoice);

		Task PushReport(SalesReport report);
	}
}
