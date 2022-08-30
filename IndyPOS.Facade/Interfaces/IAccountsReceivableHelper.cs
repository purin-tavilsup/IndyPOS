using System.Collections.Generic;

namespace IndyPOS.Facade.Interfaces
{
    public interface IAccountsReceivableHelper
	{
		IList<IAccountsReceivable> GetAccountsReceivables();

		IAccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId);

		void UpdateAccountsReceivable(IAccountsReceivable accountsReceivable);

		void ConvertPaymentsToAccountsReceivables();
	}
}
