using IndyPOS.Enums;
using IndyPOS.Sales;
using System;
using System.Collections.Generic;

namespace IndyPOS.Controllers
{
    public interface IReportController
	{
		IEnumerable<IFinalInvoice> Invoices { get; }

		IEnumerable<IFinalInvoiceProduct> InvoiceProducts { get; }

		IEnumerable<IFinalInvoicePayment> InvoicePayments { get; }

		IEnumerable<IAccountsReceivable> AccountsReceivables { get; }

		decimal GetInvoicesTotal();

		decimal GetRefundTotal();

		decimal GetArTotal();

		decimal GetCompletedArTotal();

		decimal GetIncompleteArTotal();

		decimal GetInvoicesTotalWithoutAr();

		decimal GetGeneralProductsTotal();

		decimal GetHardwareProductsTotal();

		void LoadInvoicesByPeriod(ReportPeriod period);

		void LoadInvoicesByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date);

		decimal GetPaymentsTotalByType(PaymentType type);

		void WriteSaleRecordsToCsvFileByDate(DateTime date);

		IFinalInvoice GetInvoiceByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId);

		IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId);

		SaleReport CreateSaleReport();

		PaymentReport CreatePaymentReport();

		ArReport CreateArReport();
	}
}
