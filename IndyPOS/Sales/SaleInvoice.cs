using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Sales
{
	public class SaleInvoice : ISaleInvoice
	{
        public int? Id { get; private set; }

        public IList<ISaleInvoiceProduct> Products { get; }

        public IList<IPayment> Payments { get; }

        public decimal InvoiceTotal => Products.Sum(p => p.Quantity * p.UnitPrice);

        public decimal PaymentTotal => Payments.Sum(p => p.Amount);

        public decimal BalanceRemaining => InvoiceTotal - PaymentTotal;

        public bool IsRefundInvoice => InvoiceTotal < 0;

        public decimal Changes => CalculateChanges();

        public SaleInvoice()
		{
            Products = new List<ISaleInvoiceProduct>();
            Payments = new List<IPayment>();
        }

        private decimal CalculateChanges()
        {
            var amount = PaymentTotal - InvoiceTotal;

            return amount >= 0 ? amount : 0m;
        }

        public void SetSaleInvoiceId(int id)
        {
            Id = id;
        }
    }
}
