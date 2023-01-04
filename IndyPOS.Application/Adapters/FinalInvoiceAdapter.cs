using IndyPOS.Application.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.Application.Adapters;

public class FinalInvoiceAdapter : IFinalInvoice
{
	private readonly Invoice _adaptee;

	public FinalInvoiceAdapter(Invoice adaptee)
	{
		_adaptee = adaptee;
	}

	public int InvoiceId => _adaptee.InvoiceId;

	public decimal Total => _adaptee.Total;

	public int? CustomerId => _adaptee.CustomerId;

	public int UserId => _adaptee.UserId;

	public string DateCreated => _adaptee.DateCreated;
}