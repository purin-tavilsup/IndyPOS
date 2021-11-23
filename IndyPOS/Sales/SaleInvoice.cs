using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Sales
{
	public class SaleInvoice : ISaleInvoice
	{
        public IList<ISaleInvoiceProduct> Products { get; }

        public IList<IPayment> Payments { get; }

        public decimal InvoiceTotal => Products.Sum(p => p.Quantity * p.UnitPrice);

        public decimal PaymentTotal => Payments.Sum(p => p.Amount);

        public decimal Changes => CalculateChanges();

        public int UserId { get; set; }

        public int? CustomerId { get; set; }

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
    }
}
