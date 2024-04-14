namespace IndyPOS.Application.InvoicePayments;

public record InvoicePaymentDto(
	int PaymentId,
	int InvoiceId,
	int PaymentTypeId,
	decimal Amount,
	string DateCreated,
	string Note);