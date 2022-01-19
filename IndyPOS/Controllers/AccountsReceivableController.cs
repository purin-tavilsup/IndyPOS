using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Sales;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;
using AccountsReceivableModel = IndyPOS.DataAccess.Models.AccountsReceivable;

namespace IndyPOS.Controllers
{
    public class AccountsReceivableController : IAccountsReceivableController
    {
		private readonly IEventAggregator _eventAggregator;
        private readonly IAccountsReceivableRepository _accountsReceivableRepository;

        public AccountsReceivableController(IAccountsReceivableRepository accountsReceivableRepository,
											IEventAggregator eventAggregator)
		{
			_accountsReceivableRepository = accountsReceivableRepository;
			_eventAggregator = eventAggregator;
		}

        public IList<IAccountsReceivable> GetAccountsReceivables()
		{
			var results = _accountsReceivableRepository.GetAccountsReceivables();

			return results.Select(x => new AccountsReceivableAdapter(x) as IAccountsReceivable).ToList();
		}

		public IAccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId)
		{
			var result = _accountsReceivableRepository.GetAccountsReceivableByInvoiceId(invoiceId);

			return result != null ? new AccountsReceivableAdapter(result) as IAccountsReceivable : null;
		}

        public void UpdateAccountsReceivable(IAccountsReceivable accountsReceivable)
		{
			var isCompleted = accountsReceivable.ReceivableAmount == accountsReceivable.PaidAmount;

			_accountsReceivableRepository.UpdateAccountsReceivable(new AccountsReceivableModel
																   {
																	   PaymentId = accountsReceivable.PaymentId,
																	   PaidAmount = accountsReceivable.PaidAmount,
																	   IsCompleted = isCompleted
																   });
        }
	}
}
