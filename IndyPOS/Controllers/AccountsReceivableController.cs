using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Enums;
using IndyPOS.Sales;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;
using AccountsReceivableModel = IndyPOS.DataAccess.Models.AccountsReceivable;
using PaymentModel = IndyPOS.DataAccess.Models.Payment;

namespace IndyPOS.Controllers
{
    public class AccountsReceivableController : IAccountsReceivableController
    {
		private readonly IEventAggregator _eventAggregator;
        private readonly IAccountsReceivableRepository _accountsReceivableRepository;
		private readonly IInvoiceRepository _invoicesRepository;

        public AccountsReceivableController(IAccountsReceivableRepository accountsReceivableRepository,
											IInvoiceRepository invoicesRepository,
											IEventAggregator eventAggregator)
		{
			_accountsReceivableRepository = accountsReceivableRepository;
			_invoicesRepository = invoicesRepository;
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

		public void ConvertPaymentsToAccountsReceivables()
		{
			var accountsReceivablePayments = _invoicesRepository.GetPaymentsByPaymentTypeId((int) PaymentType.AccountReceivable);
			var accountsReceivables = _accountsReceivableRepository.GetAccountsReceivables();

			var arPaymentIds = accountsReceivables.Select(ar => ar.PaymentId).ToHashSet();

			foreach (var arPayment in accountsReceivablePayments)
            {
				if (arPaymentIds.Contains(arPayment.PaymentId))
					continue;

				ConvertPaymentToAccountsReceivable(arPayment);
			}
		}

		private void ConvertPaymentToAccountsReceivable(PaymentModel payment)
		{
			_accountsReceivableRepository.ConvertPaymentToAccountsReceivable(payment);
		}
	}
}
