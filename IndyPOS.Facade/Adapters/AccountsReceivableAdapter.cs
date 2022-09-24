using IndyPOS.Facade.Interfaces;
using AccountsReceivableModel = IndyPOS.DataAccess.Models.AccountsReceivable;

namespace IndyPOS.Facade.Adapters
{
	public class AccountsReceivableAdapter : IAccountsReceivable
    {
		private readonly AccountsReceivableModel _adaptee;

		public AccountsReceivableAdapter(AccountsReceivableModel adaptee)
		{
			_adaptee = adaptee;
		}

		public int PaymentId => _adaptee.PaymentId;

		public string Description => _adaptee.Description;

		public int InvoiceId => _adaptee.InvoiceId;

		public decimal ReceivableAmount => _adaptee.ReceivableAmount;

		public decimal PaidAmount
		{
			get => _adaptee.PaidAmount; 
			set => _adaptee.PaidAmount = value;
		}

		public bool IsCompleted
		{
			get => _adaptee.IsCompleted;
			set => _adaptee.IsCompleted = value;
		}

		public string DateCreated => _adaptee.DateCreated;

		public string DateUpdated => _adaptee.DateUpdated;
	}
}
