using IndyPOS.Sales;
using System.Collections.Generic;

namespace IndyPOS.Controllers
{
    public interface IAccountsReceivableController
	{
		IList<IAccountsReceivable> GetAccountsReceivables();

		IAccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId);

		void UpdateAccountsReceivable(IAccountsReceivable accountsReceivable);
	}
}
