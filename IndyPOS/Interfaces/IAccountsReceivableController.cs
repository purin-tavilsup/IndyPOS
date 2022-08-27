﻿using System.Collections.Generic;

namespace IndyPOS.Interfaces
{
    public interface IAccountsReceivableController
	{
		IList<IAccountsReceivable> GetAccountsReceivables();

		IAccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId);

		void UpdateAccountsReceivable(IAccountsReceivable accountsReceivable);

		void ConvertPaymentsToAccountsReceivables();
	}
}