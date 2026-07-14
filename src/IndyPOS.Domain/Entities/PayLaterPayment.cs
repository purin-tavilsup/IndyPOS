namespace IndyPOS.Domain.Entities;

/// <summary>
/// Represents a Pay Later (credit) payment where a customer pays for goods at a later date.
/// Business Rule: If an invoice has a PayLater payment, it cannot have other payment types combined.
/// </summary>
public class PayLaterPayment
{
	public int PaymentId { get; set; }

	/// <summary>
	/// Customer identifier (name/description) for this credit account.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	public int InvoiceId { get; set; }

	/// <summary>
	/// Total amount owed by the customer.
	/// </summary>
	public decimal PayLaterAmount { get; set; }

	/// <summary>
	/// Amount that has been paid so far.
	/// </summary>
	public decimal PaidAmount { get; set; }

	/// <summary>
	/// Stored completion status (mapped from DB).
	/// Note: Use CalculateIsCompleted() for business logic to ensure accuracy.
	/// </summary>
	public bool IsCompleted { get; set; }

	public string DateCreated { get; set; } = string.Empty;

	public string DateUpdated { get; set; } = string.Empty;

	/// <summary>
	/// Calculates the completion status based on current amounts.
	/// Returns true when the customer has fully paid their debt.
	/// </summary>
	public bool CalculateIsCompleted() => PaidAmount >= PayLaterAmount;

	/// <summary>
	/// Returns the remaining amount the customer still owes.
	/// </summary>
	public decimal RemainingAmount => PayLaterAmount - PaidAmount;

	/// <summary>
	/// Checks if this PayLater payment would be completed with the given additional payment amount.
	/// </summary>
	public bool WouldBeCompletedWith(decimal additionalAmount) => PaidAmount + additionalAmount >= PayLaterAmount;

	/// <summary>
	/// Updates the paid amount and recalculates IsCompleted status.
	/// </summary>
	public void RecordPayment(decimal amount)
	{
		PaidAmount = amount;
		IsCompleted = CalculateIsCompleted();
	}
}