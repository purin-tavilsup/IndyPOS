using IndyPOS.DataAccess.Interfaces;
using IndyPOS.Facade.Adapters;
using IndyPOS.Facade.Interfaces;
using AccountsReceivableModel = IndyPOS.DataAccess.Models.AccountsReceivable;

namespace IndyPOS.Facade.Helpers;

public class AccountsReceivableHelper : IAccountsReceivableHelper
{
	private readonly IAccountsReceivableRepository _accountsReceivableRepository;

	public AccountsReceivableHelper(IAccountsReceivableRepository accountsReceivableRepository)
	{
		_accountsReceivableRepository = accountsReceivableRepository;
	}

	public IList<IAccountsReceivable> GetAccountsReceivables()
	{
		var results = _accountsReceivableRepository.GetAccountsReceivables();

		return results.Select(x => new AccountsReceivableAdapter(x) as IAccountsReceivable).ToList();
	}

	public IAccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId)
	{
		var result = _accountsReceivableRepository.GetAccountsReceivableByInvoiceId(invoiceId);

		return new AccountsReceivableAdapter(result);
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
}