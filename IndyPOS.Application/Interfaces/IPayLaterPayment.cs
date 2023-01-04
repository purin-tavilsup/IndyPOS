namespace IndyPOS.Application.Interfaces;

public interface IPayLaterPayment
{
	int PaymentId { get; }

	string Description { get; }

	int InvoiceId { get; }

	decimal Amount { get; }

	decimal PaidAmount { get; set; }

	bool IsCompleted { get; set; }

	string DateCreated { get; }

	string DateUpdated { get; }
}