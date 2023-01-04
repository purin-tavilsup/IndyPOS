using IndyPOS.Application.Models.Report;

namespace IndyPOS.Application.Interfaces;

public interface IDataFeedApiHelper
{
	Task PushInvoice(Invoice invoice);

	Task PushReport(SalesReport report);
}