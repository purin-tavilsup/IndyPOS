using Dapper;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class InvoicePaymentRepository : IInvoicePaymentRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public InvoicePaymentRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public int AddPayment(Payment payment)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"INSERT INTO Payments
				(
                    InvoiceId,
                    PaymentTypeId,
                    Amount,
                    DateCreated,
					Note
                )
                VALUES
                (
                    @InvoiceId,
                    @PaymentTypeId,
                    @Amount,
                    datetime('now','localtime'),
					@Note
                );
                SELECT last_insert_rowid()";

		var sqlParameters = new
		{
			payment.InvoiceId,
			payment.PaymentTypeId,
			Amount = payment.Amount.ToMoneyString(),
			payment.Note
		};

		var paymentId = connection.Query<int>(sqlCommand, sqlParameters)
								  .FirstOrDefault();

		return paymentId;
	}

	public IEnumerable<Payment> GetPaymentsByInvoiceId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                PaymentId,
                InvoiceId,
                PaymentTypeId,
                Amount,
                DateCreated,
				Note
                FROM Payments 
                WHERE InvoiceId = @invoiceId";

		var sqlParameters = new
		{
			invoiceId = id
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<Payment>() : MapPayments(results);
	}

	public IEnumerable<Payment> GetPaymentsByDate(DateTime date)
	{
		return GetPaymentsByDateRange(date, date);
	}

	public IEnumerable<Payment> GetPaymentsByDateRange(DateTime start, DateTime end)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                PaymentId,
                InvoiceId,
                PaymentTypeId,
                Amount,
                DateCreated,
				Note
                FROM Payments 
                WHERE DateCreated BETWEEN @startDate AND @endDate";

		var sqlParameters = new
		{
			startDate = start.ToStartDateString(),
			endDate = end.ToEndDateString()
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<Payment>() : MapPayments(results);
	}

	public IEnumerable<Payment> GetPaymentsByPaymentTypeId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                PaymentId,
                InvoiceId,
                PaymentTypeId,
                Amount,
                DateCreated,
				Note
                FROM Payments 
                WHERE PaymentTypeId = @PaymentTypeId";

		var sqlParameters = new
		{
			PaymentTypeId = id
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<Payment>() : MapPayments(results);
	}

	private static IEnumerable<Payment> MapPayments(IEnumerable<dynamic> results)
	{
		var payments = results.Select(x => new Payment
		{
			PaymentId = (int)x.PaymentId,
			InvoiceId = (int)x.InvoiceId,
			PaymentTypeId = (int)x.PaymentTypeId,
			Amount = ((string)x.Amount).ToMoney(),
			DateCreated = x.DateCreated,
			Note = x.Note
		});

		return payments;
	}
}