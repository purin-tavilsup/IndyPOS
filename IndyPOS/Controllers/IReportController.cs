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

		IEnumerable<IFinalInvoiceProduct> GeneralGoodsProducts { get; }

		IEnumerable<IFinalInvoiceProduct> HardwareProducts { get; }

		decimal InvoicesTotal { get; }

		decimal GeneralGoodsProductsTotal { get; }

		decimal HardwareProductsTotal { get; }


		void LoadInvoicesByPeriod(ReportPeriod period);

		void LoadInvoicesByDateRange(DateTime startDate, DateTime endDate);

		IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date);
	}
}
