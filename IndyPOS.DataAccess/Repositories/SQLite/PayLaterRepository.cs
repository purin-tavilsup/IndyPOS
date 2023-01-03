using Dapper;
using IndyPOS.Common.Exceptions;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class PayLaterRepository : IPayLaterPaymentRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public PayLaterRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public int AddPayLaterPayment(PayLaterPayment accountsReceivable)
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
			throw new PayLaterPaymentNotAddedException($"Failed to add a new account receivable. InvoiceId: {accountsReceivable.InvoiceId}.");

		return rowId;
	}

	public void UpdatePayLaterPayment(PayLaterPayment accountsReceivable)
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
			throw new PayLaterPaymentNotUpdatedException($"Failed to update an account receivable. InvoiceId: {accountsReceivable.InvoiceId}.");
	}

	public IEnumerable<PayLaterPayment> GetPayLaterPayments()
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

	public IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments()
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

		return results is null ? Enumerable.Empty<PayLaterPayment>() : MapAccountsReceivables(results);
	}

	public PayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId)
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
			throw new PayLaterPaymentNotFoundException($"Account Receivable is not found. InvoiceId: {invoiceId}.");

		return MapAccountsReceivable(result);
	}

	public PayLaterPayment GetPayLaterPaymentByPaymentId(int paymentId)
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
				WHERE PaymentId = @PaymentId";

		var sqlParameters = new
		{
			PaymentId = paymentId
		};

		var result = connection.Query(sqlCommand, sqlParameters)
							   .FirstOrDefault();

		if (result is null)
			throw new PayLaterPaymentNotFoundException($"Account Receivable is not found. PaymentId: {paymentId}.");

		return MapAccountsReceivable(result);
	}

	public IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateTime start, DateTime end)
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

		return results is null ? Enumerable.Empty<PayLaterPayment>() : MapAccountsReceivables(results);
	}

	private static PayLaterPayment MapAccountsReceivable(dynamic result)
	{
		var accountsReceivables = new PayLaterPayment
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

	private static IEnumerable<PayLaterPayment> MapAccountsReceivables(IEnumerable<dynamic> results)
	{
		return results.Select(MapAccountsReceivable);
	}
}