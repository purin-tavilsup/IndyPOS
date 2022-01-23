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

		IEnumerable<IFinalInvoiceProduct> GeneralGoodsProducts { get; }

		IEnumerable<IFinalInvoiceProduct> HardwareProducts { get; }

		IEnumerable<IAccountsReceivable> AccountsReceivables { get; }

		decimal InvoicesTotal { get; }

		decimal PaymentsTotal { get; }

		decimal ChangesTotal { get; }

		decimal RefundTotal { get; }

		decimal ArTotal { get; }

		decimal CompletedArTotal { get; }

		decimal IncompleteArTotal { get; }


		decimal InvoicesTotalWithoutAr { get; }

		decimal InvoicesTotalWithoutIncompleteAr { get; }

		decimal GeneralGoodsProductsTotal { get; }

		decimal HardwareProductsTotal { get; }


		void LoadInvoicesByPeriod(ReportPeriod period);

		void LoadInvoicesByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date);

		decimal GetPaymentsTotalByType(PaymentType type);

		void WriteSaleRecordsToCsvFileByDate(DateTime date);
	}
}
