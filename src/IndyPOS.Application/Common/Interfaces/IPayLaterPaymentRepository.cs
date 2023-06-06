using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IPayLaterPaymentRepository
{
	int Add(PayLaterPayment payment);

	bool Update(PayLaterPayment payment);

	bool RemoveById(int id);

	IEnumerable<PayLaterPayment> GetAll();

	PayLaterPayment GetById(int id);

	IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments();

	PayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId);

	IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateOnly start, DateOnly end);
}