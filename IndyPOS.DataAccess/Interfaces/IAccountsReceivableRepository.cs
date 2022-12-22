using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IAccountsReceivableRepository
{
	int AddAccountsReceivable(AccountsReceivable accountsReceivable);

	void UpdateAccountsReceivable(AccountsReceivable accountsReceivable);

	IEnumerable<AccountsReceivable> GetAccountsReceivables();

	IEnumerable<AccountsReceivable> GetIncompleteAccountsReceivables();

	AccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId);

	IEnumerable<AccountsReceivable> GetAccountsReceivablesByDateRange(DateTime start, DateTime end);
}