using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IPayLaterPaymentRepository
{
	int AddPayLaterPayment(PayLaterPayment payment);

	void UpdatePayLaterPayment(PayLaterPayment payment);

	IEnumerable<PayLaterPayment> GetPayLaterPayments();

	IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments();

	PayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId);

	PayLaterPayment GetPayLaterPaymentByPaymentId(int paymentId);

	IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateTime start, DateTime end);
}