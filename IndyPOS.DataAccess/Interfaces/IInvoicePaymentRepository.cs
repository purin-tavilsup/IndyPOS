using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IInvoicePaymentRepository
{
	int AddPayment(Payment payment);

	IEnumerable<Payment> GetPaymentsByInvoiceId(int id);

	IEnumerable<Payment> GetPaymentsByDateRange(DateTime start, DateTime end);

	IEnumerable<Payment> GetPaymentsByDate(DateTime date);

	IEnumerable<Payment> GetPaymentsByPaymentTypeId(int id);
}