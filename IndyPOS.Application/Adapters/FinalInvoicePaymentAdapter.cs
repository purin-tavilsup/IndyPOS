using IndyPOS.Application.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.Application.Adapters;

public class FinalInvoicePaymentAdapter : IFinalInvoicePayment
{
	private readonly Payment _adaptee;

	public FinalInvoicePaymentAdapter(Payment adaptee)
	{
		_adaptee = adaptee;
	}

	public int PaymentId => _adaptee.PaymentId;

	public int InvoiceId => _adaptee.InvoiceId;

	public int PaymentTypeId => _adaptee.PaymentTypeId;

	public decimal Amount => _adaptee.Amount;

	public string DateCreated => _adaptee.DateCreated;

	public string Note => _adaptee.Note;
}