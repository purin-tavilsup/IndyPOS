using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Abstractions.Reports.Repositories;

public interface IReportRepository
{
	Task AddSalesReportAsync(SalesReport report);

	Task AddPaymentsReportAsync(PaymentsReport report);
}