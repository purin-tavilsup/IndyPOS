using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Adapters;

public class PayLaterPaymentAdapter : IPayLaterPayment
{
	private readonly PayLaterPayment _adaptee;

	public PayLaterPaymentAdapter(PayLaterPayment adaptee)
	{
		_adaptee = adaptee;
	}

	public int PaymentId => _adaptee.PaymentId;

	public string Description => _adaptee.Description;

	public int InvoiceId => _adaptee.InvoiceId;

	public decimal Amount => _adaptee.ReceivableAmount;

	public decimal PaidAmount
	{
		get => _adaptee.PaidAmount; 
		set => _adaptee.PaidAmount = value;
	}

	public bool IsCompleted
	{
		get => _adaptee.IsCompleted;
		set => _adaptee.IsCompleted = value;
	}

	public string DateCreated => _adaptee.DateCreated;

	public string DateUpdated => _adaptee.DateUpdated;
}