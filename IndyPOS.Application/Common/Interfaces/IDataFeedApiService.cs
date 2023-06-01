using IndyPOS.Application.Common.Models.Report;

namespace IndyPOS.Application.Common.Interfaces;

public interface IDataFeedApiService
{
	Task PushInvoice(Invoice invoice);

	Task PushReport(SalesReport report);
}