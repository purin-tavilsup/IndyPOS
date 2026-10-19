using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// StoreHub-based implementation of IReportService.
/// Calls StoreHub API endpoints instead of SQLite.
/// </summary>
public class StoreHubReportService : IReportService
{
    private readonly IStoreHubClient _storeHubClient;

    public StoreHubReportService(IStoreHubClient storeHubClient)
    {
        _storeHubClient = storeHubClient;
    }

    public async Task<SalesSummary> CreateSalesSummaryByPeriodAsync(TimePeriod period)
    {
        var dateRange = period.ToDateRange();
        return await CreateSalesSummaryByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
    }

    public async Task<SalesSummary> CreateSalesSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _storeHubClient.GetLegacySalesSummaryAsync(startDate, endDate);
    }

    public async Task<PaymentsSummary> CreatePaymentsSummaryByPeriodAsync(TimePeriod period)
    {
        var dateRange = period.ToDateRange();
        return await CreatePaymentsSummaryByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
    }

    public async Task<PaymentsSummary> CreatePaymentsSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _storeHubClient.GetLegacyPaymentsSummaryAsync(startDate, endDate);
    }
}
