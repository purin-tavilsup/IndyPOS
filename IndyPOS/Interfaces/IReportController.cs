using IndyPOS.Enums;
using IndyPOS.Sales;
using System;
using System.Collections.Generic;

namespace IndyPOS.Interfaces
{
	public interface IReportController
	{
		IEnumerable<IFinalInvoice> Invoices { get; }

		IEnumerable<IFinalInvoiceProduct> InvoiceProducts { get; }

		IEnumerable<IFinalInvoicePayment> InvoicePayments { get; }

		void LoadInvoicesByPeriod(ReportPeriod period);

		void LoadInvoicesByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date);

		void WriteSaleRecordsToCsvFileByDate(DateTime date);

		IFinalInvoice GetInvoiceByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId);

		SalesReport GetSaleReport();

		PaymentReport GetPaymentReport();

		ArReport GetArReport();
	}
}
