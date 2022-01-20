using IndyPOS.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IAccountsReceivableRepository
	{
		int AddAccountsReceivable(AccountsReceivable accountsReceivable);

		void UpdateAccountsReceivable(AccountsReceivable accountsReceivable);

		IEnumerable<AccountsReceivable> GetAccountsReceivables();

		IEnumerable<AccountsReceivable> GetIncompleteAccountsReceivables();

		AccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId);

		IEnumerable<AccountsReceivable> GetAccountsReceivablesByDateRange(DateTime start, DateTime end);

		void ConvertPaymentToAccountsReceivable(Payment payment);
	}
}
