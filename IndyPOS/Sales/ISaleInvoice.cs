using System.Collections.Generic;

namespace IndyPOS.Sales
{
	public interface ISaleInvoice
	{
        IList<ISaleInvoiceProduct> Products { get; }

        IList<IPayment> Payments { get; }

        decimal InvoiceTotal { get; }

        decimal PaymentTotal { get; }

        decimal Changes { get; }

        int UserId { get; set; }

        int? CustomerId { get; set; }
    }
}