using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IAccountsReceivableRepository
	{
		int AddAccountsReceivable(AccountsReceivable accountsReceivable);

		void UpdateAccountsReceivable(AccountsReceivable accountsReceivable);

		IEnumerable<AccountsReceivable> GetAccountsReceivables();

		AccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId);
	}
}
