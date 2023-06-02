namespace IndyPOS.Application.Invoices;

public class InvoiceDto
{
	public int InvoiceId { get; set; }

	public decimal Total { get; set; }

	public int? CustomerId { get; set; }

	public int UserId { get; set; }

	public string DateCreated { get; set; } = string.Empty;
}