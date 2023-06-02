using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Domain.Entities;

[ExcludeFromCodeCoverage]
public class PayLaterPayment
{
	public int PaymentId { get; set; }

	public string Description { get; set; } = string.Empty;

	public int InvoiceId { get; set; }

	public decimal ReceivableAmount { get; set; }

	public decimal PaidAmount { get; set; }

	public bool IsCompleted { get; set; }

	public string DateCreated { get; set; } = string.Empty;

	public string DateUpdated { get; set; } = string.Empty;
}