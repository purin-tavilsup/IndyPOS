using IndyPOS.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
	public interface IInvoiceRepository
    {
        int AddInvoice(Invoice invoice);

        Invoice GetInvoiceByInvoiceId(int id);

        IList<Invoice> GetInvoicesByDateRange(DateTime start, DateTime end);

        IList<Invoice> GetInvoicesByDate(DateTime date);

        int AddInvoiceProduct(InvoiceProduct product);

        IList<InvoiceProduct> GetInvoiceProductsByInvoiceId(int id);

        IList<InvoiceProduct> GetInvoiceProductsByDateRange(DateTime start, DateTime end);

        IList<InvoiceProduct> GetInvoiceProductsByDate(DateTime date);

        int AddPayment(Payment payment);

        IList<Payment> GetPaymentsByInvoiceId(int id);

		IList<Payment> GetPaymentsByDateRange(DateTime start, DateTime end);

		IList<Payment> GetPaymentsByDate(DateTime date);
	}
}
