using IndyPOS.Common.Enums;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.Facade.Adapters;
using IndyPOS.Facade.Interfaces;
using Prism.Events;
using AccountsReceivableModel = IndyPOS.DataAccess.Models.AccountsReceivable;
using PaymentModel = IndyPOS.DataAccess.Models.Payment;

namespace IndyPOS.Facade.Helpers;

public class AccountsReceivableHelper : IAccountsReceivableHelper
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IAccountsReceivableRepository _accountsReceivableRepository;
	private readonly IInvoiceRepository _invoicesRepository;

	public AccountsReceivableHelper(IAccountsReceivableRepository accountsReceivableRepository,
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

	public IEnumerable<IAccountsReceivable> GetAccountsReceivablesByDateRange(DateTime startDate, DateTime endDate)
	{
		var results = _accountsReceivableRepository.GetAccountsReceivablesByDateRange(startDate, endDate);

		return results.Select(x => new AccountsReceivableAdapter(x) as IAccountsReceivable);
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