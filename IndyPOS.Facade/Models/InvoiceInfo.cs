using IndyPOS.Facade.Interfaces;
using System.Collections.Generic;

namespace IndyPOS.Facade.Models
{
	internal class InvoiceInfo : IInvoiceInfo
    {
		public int Id { get; set; }

		public IList<ISaleInvoiceProduct> Products { get; set; }

		public IList<IPayment> Payments { get; set; }

		public bool IsRefundInvoice { get; set; }

		public decimal InvoiceTotal { get; set; }

		public decimal PaymentTotal { get; set; }

		public decimal Changes { get; set; }
    }
}
