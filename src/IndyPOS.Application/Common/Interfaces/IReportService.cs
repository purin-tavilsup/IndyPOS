using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Common.Interfaces;

public interface IReportService
{
	Task<SalesSummary> CreateSalesSummaryByPeriodAsync(TimePeriod period);

	Task<SalesSummary> CreateSalesSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<PaymentsSummary> CreatePaymentsSummaryByPeriodAsync(TimePeriod period);

	Task<PaymentsSummary> CreatePaymentsSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate);
}