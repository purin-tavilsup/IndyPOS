namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoiceInfo
{
	int Id { get; set; }

	IList<ISaleInvoiceProduct> Products { get; }

	IList<IPayment> Payments { get; }

	bool IsRefundInvoice { get; }

	decimal InvoiceTotal { get; }

	decimal PaymentTotal { get; }

	decimal Changes { get; }
}