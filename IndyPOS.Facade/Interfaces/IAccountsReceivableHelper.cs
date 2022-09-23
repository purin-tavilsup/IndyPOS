using System;
using System.Collections.Generic;

namespace IndyPOS.Facade.Interfaces
{
    public interface IAccountsReceivableHelper
	{
		IList<IAccountsReceivable> GetAccountsReceivables();

		IAccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId);

		IEnumerable<IAccountsReceivable> GetAccountsReceivablesByDateRange(DateTime startDate, DateTime endDate);

		void UpdateAccountsReceivable(IAccountsReceivable accountsReceivable);

		void ConvertPaymentsToAccountsReceivables();
	}
}
