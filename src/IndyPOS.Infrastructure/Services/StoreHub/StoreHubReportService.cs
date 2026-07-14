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

    // ========================
    // Methods below return empty/throw - they use legacy DTOs with int IDs
    // These should be migrated to use Guid-based StoreHub queries
    // For now, return empty to avoid breaking the build
    // ========================

    public Task<IEnumerable<InvoiceDto>> GetInvoicesByPeriodAsync(TimePeriod period)
    {
        // TODO: Implement via StoreHub GetInvoicesQuery when UI is migrated
        return Task.FromResult(Enumerable.Empty<InvoiceDto>());
    }

    public Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        // TODO: Implement via StoreHub GetInvoicesQuery when UI is migrated
        return Task.FromResult(Enumerable.Empty<InvoiceDto>());
    }

    public Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsByPeriodAsync(TimePeriod period)
    {
        // TODO: Implement via StoreHub GetPayLaterReportQuery when UI is migrated
        return Task.FromResult(Enumerable.Empty<PayLaterPaymentDto>());
    }

    public Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsAsync()
    {
        // TODO: Implement via StoreHub GetPayLaterQuery when UI is migrated
        return Task.FromResult(Enumerable.Empty<PayLaterPaymentDto>());
    }

    public Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateAsync(DateOnly date)
    {
        // TODO: Implement via StoreHub when UI is migrated
        return Task.FromResult(Enumerable.Empty<InvoiceProductDto>());
    }

    public Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        // TODO: Implement via StoreHub when UI is migrated
        return Task.FromResult(Enumerable.Empty<InvoiceProductDto>());
    }

    public Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByInvoiceIdAsync(int invoiceId)
    {
        // Legacy int-based ID - not supported in StoreHub mode
        return Task.FromResult(Enumerable.Empty<InvoiceProductDto>());
    }

    public Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId)
    {
        // Legacy int-based ID - not supported in StoreHub mode
        return Task.FromResult(Enumerable.Empty<InvoicePaymentDto>());
    }

    public Task<IInvoiceInfo> GetInvoiceInfoAsync(int invoiceId)
    {
        // Legacy int-based ID - not supported in StoreHub mode
        // Return a stub implementation
        return Task.FromResult<IInvoiceInfo>(new StubInvoiceInfo());
    }

    private class StubInvoiceInfo : IInvoiceInfo
    {
        public int Id => 0;
        public Guid? StoreHubInvoiceId => null;
        public IList<Product> Products => new List<Product>();
        public IList<Payment> Payments => new List<Payment>();
        public bool IsRefundInvoice => false;
        public decimal InvoiceTotal => 0;
        public decimal PaymentTotal => 0;
        public decimal Changes => 0;
        public bool HasPayLaterPayment => false;
    }
}
