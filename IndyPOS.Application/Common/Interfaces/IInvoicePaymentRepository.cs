using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoicePaymentRepository
{
	int Add(Payment payment);

	IEnumerable<Payment> GetByInvoiceId(int id);

	IEnumerable<Payment> GetByDateRange(DateTime start, DateTime end);

	IEnumerable<Payment> GetByDate(DateTime date);

	IEnumerable<Payment> GetByPaymentTypeId(int id);
}