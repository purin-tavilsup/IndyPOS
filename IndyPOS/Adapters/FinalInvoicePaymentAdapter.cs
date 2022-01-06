using IndyPOS.Sales;
using PaymentModel = IndyPOS.DataAccess.Models.Payment;

namespace IndyPOS.Adapters
{
    internal class FinalInvoicePaymentAdapter : IFinalInvoicePayment
    {
		private readonly PaymentModel _adaptee;

		public FinalInvoicePaymentAdapter(PaymentModel adaptee)
		{
			_adaptee = adaptee;
		}

		public int PaymentId => _adaptee.PaymentId;

		public int InvoiceId => _adaptee.InvoiceId;

		public int PaymentTypeId => _adaptee.PaymentTypeId;

		public decimal Amount => _adaptee.Amount;

		public string DateCreated => _adaptee.DateCreated;

		public string Note => _adaptee.Note;
	}
}
