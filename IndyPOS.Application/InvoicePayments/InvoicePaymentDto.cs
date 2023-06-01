namespace IndyPOS.Application.InvoicePayments;

public class InvoicePaymentDto
{
	public int PaymentId { get; set; }

	public int InvoiceId { get; set; }

	public int PaymentTypeId { get; set; }

	public decimal Amount { get; set; }

	public string DateCreated { get; set; } = string.Empty;

	public string Note { get; set; } = string.Empty;
}