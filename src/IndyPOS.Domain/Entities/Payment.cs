using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Domain.Entities;

[ExcludeFromCodeCoverage]
public class Payment
{
	public int PaymentId { get; set; }

	public int InvoiceId { get; set; }

	public int PaymentTypeId { get; set; }

	public decimal Amount { get; set; }

	public string DateCreated { get; set; } = string.Empty;

	public string Note { get; set; } = string.Empty;
}