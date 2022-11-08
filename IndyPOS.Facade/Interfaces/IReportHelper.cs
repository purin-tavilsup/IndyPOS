using IndyPOS.Common.Enums;
using IndyPOS.Facade.Models.Report;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IndyPOS.Facade.Interfaces
{
	public interface IReportHelper
	{
		Task<SalesReport> GetSalesReportAsync();

		Task<PaymentsReport> GetPaymentsReportAsync();

		ArReport GetArReport();

		IEnumerable<IFinalInvoice> GetInvoicesByPeriod(TimePeriod period);

		IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId);

		SalesSummary CreateSalesSummary(IInvoiceInfo invoiceInfo);

		PaymentsSummary CreatePaymentsSummary(IInvoiceInfo invoiceInfo);

		Task UpdateReportAsync(IInvoiceInfo invoiceInfo);
	}
}
