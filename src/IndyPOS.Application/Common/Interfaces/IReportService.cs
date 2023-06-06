using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.InvoicePayments;
using IndyPOS.Application.InvoiceProducts;
using IndyPOS.Application.Invoices;

namespace IndyPOS.Application.Common.Interfaces;

public interface IReportService
{
	Task<ISalesReport> CreateSalesReportByPeriodAsync(TimePeriod period);

	Task<IPaymentsReport> CreatePaymentsReportByPeriodAsync(TimePeriod period);

	Task<IEnumerable<InvoiceDto>> GetInvoicesByPeriodAsync(TimePeriod period);

	Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateAsync(DateOnly date);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateRangeAsync(DateOnly startDate, DateOnly endDate);

	Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByInvoiceIdAsync(int invoiceId);

	Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId);
}