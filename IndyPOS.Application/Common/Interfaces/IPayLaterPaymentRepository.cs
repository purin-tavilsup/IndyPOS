using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IPayLaterPaymentRepository
{
	int AddPayLaterPayment(PayLaterPayment payment);

	bool UpdatePayLaterPayment(PayLaterPayment payment);

	IEnumerable<PayLaterPayment> GetPayLaterPayments();

	IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments();

	PayLaterPayment? GetPayLaterPaymentByInvoiceId(int invoiceId);

	PayLaterPayment? GetPayLaterPaymentByPaymentId(int paymentId);

	IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateTime start, DateTime end);
}