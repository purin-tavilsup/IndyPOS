using Dapper;
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

		return rowId;
	}

	public bool UpdatePayLaterPayment(PayLaterPayment payLaterPayment)
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
			payLaterPayment.PaymentId,
			PaidAmount = payLaterPayment.PaidAmount.ToMoneyString(),
			IsCompleted = payLaterPayment.IsCompleted ? 1 : 0
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount == 1;
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

		return MapPayLaterPayments(results);
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

		return results is null ? Enumerable.Empty<PayLaterPayment>() : MapPayLaterPayments(results);
	}

	public PayLaterPayment? GetPayLaterPaymentByInvoiceId(int invoiceId)
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

		return result is null ? null : MapPayLaterPayment(result);
	}

	public PayLaterPayment? GetPayLaterPaymentByPaymentId(int paymentId)
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

		return result is null ? null : MapPayLaterPayment(result);
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

		return results is null ? Enumerable.Empty<PayLaterPayment>() : MapPayLaterPayments(results);
	}

	private static PayLaterPayment MapPayLaterPayment(dynamic result)
	{
		var payment = new PayLaterPayment
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

		return payment;
	}

	private static IEnumerable<PayLaterPayment> MapPayLaterPayments(IEnumerable<dynamic> results)
	{
		return results.Select(MapPayLaterPayment);
	}
}