using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.InvoicePayments;
using IndyPOS.Application.UseCases.InvoiceProducts;
using IndyPOS.Application.UseCases.Invoices;
using IndyPOS.Application.UseCases.PayLaterPayments;

namespace IndyPOS.Application.Common.Interfaces;

public interface IReportService
{
	Task<SalesSummary> CreateSalesSummaryByPeriodAsync(TimePeriod period);

	Task<SalesSummary> CreateSalesSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<PaymentsSummary> CreatePaymentsSummaryByPeriodAsync(TimePeriod period);

	Task<PaymentsSummary> CreatePaymentsSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceDto>> GetInvoicesByPeriodAsync(TimePeriod period);

	Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsByPeriodAsync(TimePeriod period);

	Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateAsync(DateOnly date);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByInvoiceIdAsync(int invoiceId);

	Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId);

	Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsAsync();

	Task<SalesReport> CreateSalesReportByInvoiceIdAsync(int invoiceId, bool hasPayLaterPayment);

	Task<PaymentsReport> CreatePaymentsReportByInvoiceIdAsync(int invoiceId);

	Task<IInvoiceInfo> GetInvoiceInfoAsync(int invoiceId);
}