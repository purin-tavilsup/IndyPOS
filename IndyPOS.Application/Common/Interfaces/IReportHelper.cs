using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models.Report;

namespace IndyPOS.Application.Common.Interfaces;

public interface IReportHelper
{
	Task<SalesReport> GetSalesReportAsync();

	Task<PaymentsReport> GetPaymentsReportAsync();

	PayLaterPaymentsReport GetArReport();

	IEnumerable<IFinalInvoice> GetInvoicesByPeriod(TimePeriod period);

	IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateOnly startDate, DateOnly endDate);

	IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateOnly date);

	IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateOnly startDate, DateOnly endDate);

	IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId);

	IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId);

	SalesSummary CreateSalesSummary(IInvoiceInfo invoiceInfo);

	PaymentsSummary CreatePaymentsSummary(IInvoiceInfo invoiceInfo);

	Task UpdateReportAsync(IInvoiceInfo invoiceInfo);
}