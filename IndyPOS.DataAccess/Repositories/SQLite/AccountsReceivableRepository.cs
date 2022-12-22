using Dapper;
using IndyPOS.Common.Exceptions;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class AccountsReceivableRepository : IAccountsReceivableRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public AccountsReceivableRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public int AddAccountsReceivable(AccountsReceivable accountsReceivable)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"INSERT INTO AccountsReceivables
                (
                    PaymentId,
                    Description,
                    InvoiceId,
                    ReceivableAmount,
					DateCreated
                )
                VALUES
                (
                    @PaymentId,
                    @Description,
                    @InvoiceId,
					@ReceivableAmount,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

		var sqlParameters = new
		{
			accountsReceivable.PaymentId,
			accountsReceivable.Description,
			accountsReceivable.InvoiceId,
			ReceivableAmount = accountsReceivable.ReceivableAmount.ToMoneyString()
		};

		var rowId = connection.Query<int>(sqlCommand, sqlParameters)
							  .FirstOrDefault();

		if (rowId < 1) 
			throw new AccountReceivableNotAddedException($"Failed to add a new account receivable. InvoiceId: {accountsReceivable.InvoiceId}.");

		return rowId;
	}

	public void UpdateAccountsReceivable(AccountsReceivable accountsReceivable)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"UPDATE AccountsReceivables
                SET
                    PaidAmount = @PaidAmount,
                    IsCompleted = @IsCompleted,
                    DateUpdated = datetime('now','localtime')
                WHERE PaymentId = @PaymentId";

		var sqlParameters = new
		{
			accountsReceivable.PaymentId,
			PaidAmount = accountsReceivable.PaidAmount.ToMoneyString(),
			IsCompleted = accountsReceivable.IsCompleted ? 1 : 0
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new AccountReceivableNotUpdatedException($"Failed to update an account receivable. InvoiceId: {accountsReceivable.InvoiceId}.");
	}

	public IEnumerable<AccountsReceivable> GetAccountsReceivables()
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
				PaymentId,
				Description,
				InvoiceId,
				ReceivableAmount,
				PaidAmount,
				IsCompleted,
				DateCreated,
				DateUpdated
                FROM AccountsReceivables";

		var results = connection.Query(sqlCommand);

		return MapAccountsReceivables(results);
	}

	public IEnumerable<AccountsReceivable> GetIncompleteAccountsReceivables()
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
				PaymentId,
				Description,
				InvoiceId,
				ReceivableAmount,
				PaidAmount,
				IsCompleted,
				DateCreated,
				DateUpdated
                FROM AccountsReceivables
				WHERE IsCompleted = @IsCompleted";

		var sqlParameters = new
		{
			IsCompleted = 0
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<AccountsReceivable>() : MapAccountsReceivables(results);
	}

	public AccountsReceivable GetAccountsReceivableByInvoiceId(int invoiceId)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
				PaymentId,
				Description,
				InvoiceId,
				ReceivableAmount,
				PaidAmount,
				IsCompleted,
				DateCreated,
				DateUpdated
                FROM AccountsReceivables
				WHERE InvoiceId = @InvoiceId";

		var sqlParameters = new
		{
			InvoiceId = invoiceId
		};

		var result = connection.Query(sqlCommand, sqlParameters)
							   .FirstOrDefault();

		if (result is null)
			throw new AccountReceivableNotFoundException($"Account Receivable is not found. InvoiceId: {invoiceId}.");

		return MapAccountsReceivable(result);
	}

	public IEnumerable<AccountsReceivable> GetAccountsReceivablesByDateRange(DateTime start, DateTime end)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                PaymentId,
				Description,
				InvoiceId,
				ReceivableAmount,
				PaidAmount,
				IsCompleted,
				DateCreated,
				DateUpdated
                FROM AccountsReceivables
                WHERE DateCreated BETWEEN @startDate AND @endDate";

		var sqlParameters = new
		{
			startDate = start.ToStartDateString(),
			endDate = end.ToEndDateString()
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<AccountsReceivable>() : MapAccountsReceivables(results);
	}

	private static AccountsReceivable MapAccountsReceivable(dynamic result)
	{
		var accountsReceivables = new AccountsReceivable
		{
			PaymentId = (int)result.PaymentId,
			Description = result.Description,
			InvoiceId = (int)result.InvoiceId,
			ReceivableAmount = ((string)result.ReceivableAmount).ToMoney(),
			PaidAmount = ((string)result.PaidAmount).ToMoney(),
			IsCompleted = result.IsCompleted == 1,
			DateCreated = result.DateCreated,
			DateUpdated = result.DateUpdated
		};

		return accountsReceivables;
	}

	private static IEnumerable<AccountsReceivable> MapAccountsReceivables(IEnumerable<dynamic> results)
	{
		return results.Select(MapAccountsReceivable);
	}
}