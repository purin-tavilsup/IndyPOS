using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IInvoicePaymentRepository
{
	int AddPayment(Payment payment);

	IList<Payment> GetPaymentsByInvoiceId(int id);

	IList<Payment> GetPaymentsByDateRange(DateTime start, DateTime end);

	IList<Payment> GetPaymentsByDate(DateTime date);

	IList<Payment> GetPaymentsByPaymentTypeId(int id);
}