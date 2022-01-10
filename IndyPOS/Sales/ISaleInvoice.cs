using System.Collections.Generic;

namespace IndyPOS.Sales
{
	public interface ISaleInvoice
	{
        int? Id { get; }

		IList<ISaleInvoiceProduct> Products { get; }

        IList<IPayment> Payments { get; }

        decimal InvoiceTotal { get; }

        decimal PaymentTotal { get; }

		decimal BalanceRemaining { get; }

		bool IsRefundInvoice { get; }

        decimal Changes { get; }

        void SetSaleInvoiceId(int id);
    }
}