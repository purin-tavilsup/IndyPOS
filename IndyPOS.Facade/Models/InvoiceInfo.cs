using IndyPOS.Facade.Interfaces;

namespace IndyPOS.Facade.Models;

internal class InvoiceInfo : IInvoiceInfo
{
	public int Id { get; set; }

	public IList<ISaleInvoiceProduct> Products { get; set; } = new List<ISaleInvoiceProduct>();

	public IList<IPayment> Payments { get; set; } = new List<IPayment>();

	public bool IsRefundInvoice { get; set; }

	public decimal InvoiceTotal { get; set; }

	public decimal PaymentTotal { get; set; }

	public decimal Changes { get; set; }
}