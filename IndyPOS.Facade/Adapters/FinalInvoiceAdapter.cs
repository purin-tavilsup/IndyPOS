using IndyPOS.Facade.Interfaces;
using FinalInvoiceModel = IndyPOS.DataAccess.Models.Invoice;

namespace IndyPOS.Facade.Adapters
{
	public class FinalInvoiceAdapter : IFinalInvoice
    {
		private readonly FinalInvoiceModel _adaptee;

		public FinalInvoiceAdapter(FinalInvoiceModel adaptee)
		{
			_adaptee = adaptee;
		}

		public int InvoiceId => _adaptee.InvoiceId;

		public decimal Total => _adaptee.Total;

		public int? CustomerId => _adaptee.CustomerId;

		public int UserId => _adaptee.UserId;

		public string DateCreated => _adaptee.DateCreated;
	}
}
