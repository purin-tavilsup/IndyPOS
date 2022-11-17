using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models;

[ExcludeFromCodeCoverage]
public class AccountsReceivable
{
	public int PaymentId { get; set; }

	public string Description { get; set; }

	public int InvoiceId { get; set; }

	public decimal ReceivableAmount { get; set; }

	public decimal PaidAmount { get; set; }

	public bool IsCompleted { get; set; }

	public string DateCreated { get; set; }

	public string DateUpdated { get; set; }
}