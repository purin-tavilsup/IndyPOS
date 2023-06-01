using Dapper;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using IndyPOS.Infrastructure.Extensions;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class InvoicePaymentRepository : IInvoicePaymentRepository
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    public InvoicePaymentRepository(IDbConnectionProvider dbConnectionProvider)
    {
        _dbConnectionProvider = dbConnectionProvider;
    }

    public int Add(Payment payment)
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

    public IEnumerable<Payment> GetByInvoiceId(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM Payments WHERE InvoiceId = @invoiceId";

        var sqlParameters = new
        {
            invoiceId = id
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<Payment>() : MapPayments(results);
    }

    public IEnumerable<Payment> GetByDate(DateOnly date)
    {
        return GetByDateRange(date, date);
    }

    public IEnumerable<Payment> GetByDateRange(DateOnly start, DateOnly end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM Payments WHERE DateCreated BETWEEN @startDate AND @endDate";

        var sqlParameters = new
        {
            startDate = start.ToStartDateString(),
            endDate = end.ToEndDateString()
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<Payment>() : MapPayments(results);
    }

    public IEnumerable<Payment> GetByPaymentTypeId(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM Payments PaymentTypeId = @PaymentTypeId";

        var sqlParameters = new
        {
            PaymentTypeId = id
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<Payment>() : MapPayments(results);
    }

	public bool RemoveById(int id)
	{
		return RemoveByIdInternal(id);
	}

	private bool RemoveByIdInternal(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"DELETE FROM Payments WHERE PaymentId = @PaymentId";

		var sqlParameters = new
		{
			PaymentId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount == 1;
	}

	public bool RemoveByInvoiceId(int id)
	{
		return RemoveByInvoiceIdInternal(id);
	}

	private bool RemoveByInvoiceIdInternal(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"DELETE FROM InvoiceProducts WHERE InvoiceId = @InvoiceId";

		var sqlParameters = new
		{
			InvoiceId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount >= 0;
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