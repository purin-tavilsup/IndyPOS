using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.UseCases.Invoices;

public class InvoiceInfo: IInvoiceInfo
{
	public int Id { get; init; }
	public IList<Product> Products { get; init; } = new List<Product>();
	public IList<Payment> Payments { get; init; } = new List<Payment>();
	public bool IsRefundInvoice { get; init; }
	public decimal InvoiceTotal { get; init; }
	public decimal PaymentTotal { get; init; }
	public decimal Changes { get; init; }
	public bool HasPayLaterPayment { get; init; }
}