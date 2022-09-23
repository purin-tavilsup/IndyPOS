using IndyPOS.Common.Enums;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IndyPOS.Interfaces
{
	public interface IReportController
	{
		IEnumerable<IFinalInvoice> Invoices { get; }

		IEnumerable<IFinalInvoiceProduct> InvoiceProducts { get; }

		IEnumerable<IFinalInvoicePayment> InvoicePayments { get; }

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date);

		IFinalInvoice GetInvoiceByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId);

		Task<SalesReport> GetSaleReportAsync();

		Task<PaymentsReport> GetPaymentsReportAsync();

		ArReport GetArReport();
	}
}
