namespace IndyPOS.Application.UseCases.PayLaterPayments;

public record PayLaterPaymentDto(
	int PaymentId,
	string Description,
	int InvoiceId,
	decimal ReceivableAmount,
	decimal PaidAmount,
	bool IsCompleted,
	string DateCreated,
	string DateUpdated)
{
	/// <summary>
	/// Calculates if this PayLater would be completed with the given paid amount.
	/// </summary>
	public bool WouldBeCompletedWith(decimal paidAmount) => paidAmount >= ReceivableAmount;

	/// <summary>
	/// Returns the remaining amount the customer still owes.
	/// </summary>
	public decimal RemainingAmount => ReceivableAmount - PaidAmount;
}