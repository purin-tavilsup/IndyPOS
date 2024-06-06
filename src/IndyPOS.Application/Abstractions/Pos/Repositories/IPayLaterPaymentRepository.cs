using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Abstractions.Pos.Repositories;

public interface IPayLaterPaymentRepository
{
	int Add(PayLaterPayment payment);

	bool Update(PayLaterPayment payment);

	bool RemoveById(int id);

	IEnumerable<PayLaterPayment> GetAll();

	PayLaterPayment GetById(int id);

	IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments();

	IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDescriptionKeyword(string keyword);

	PayLaterPayment? GetPayLaterPaymentByInvoiceId(int invoiceId);

	IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateOnly start, DateOnly end);
}