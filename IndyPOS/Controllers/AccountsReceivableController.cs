﻿using IndyPOS.Facade.Interfaces;
using IndyPOS.Interfaces;

namespace IndyPOS.Controllers;

public class AccountsReceivableController : IAccountsReceivableController
{
	private readonly IAccountsReceivableHelper _accountsReceivableHelper;

	public AccountsReceivableController(IAccountsReceivableHelper accountsReceivableHelper)
	{
		_accountsReceivableHelper = accountsReceivableHelper;
	}

	public IList<IAccountsReceivable> GetAccountsReceivables()
	{
		return _accountsReceivableHelper.GetAccountsReceivables();
	}

	public IAccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId)
	{
		return _accountsReceivableHelper.GetAccountsReceivableByInvoiceId(invoiceId);
	}

	public void UpdateAccountsReceivable(IAccountsReceivable accountsReceivable)
	{
		_accountsReceivableHelper.UpdateAccountsReceivable(accountsReceivable);
	}
}