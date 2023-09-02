using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.InvoicePayments;
using IndyPOS.Application.InvoiceProducts;
using IndyPOS.Application.Invoices;
using IndyPOS.Application.PayLaterPayments;

namespace IndyPOS.Application.Common.Interfaces;

public interface IReportService
{
	Task<ISalesReport> CreateSalesReportByPeriodAsync(TimePeriod period);

	Task<ISalesReport> CreateSalesReportByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IPaymentsReport> CreatePaymentsReportByPeriodAsync(TimePeriod period);

	Task<IPaymentsReport> CreatePaymentsReportByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceDto>> GetInvoicesByPeriodAsync(TimePeriod period);

	Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsByPeriodAsync(TimePeriod period);

	Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateAsync(DateOnly date);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByInvoiceIdAsync(int invoiceId);

	Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId);

	Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsAsync();
}