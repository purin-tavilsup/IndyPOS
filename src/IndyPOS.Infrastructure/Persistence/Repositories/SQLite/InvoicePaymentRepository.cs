using Dapper;
using IndyPOS.Application.Abstractions.Pos.Repositories;
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

        const string sqlCommand = """
                                  INSERT INTO Payment
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
                                       SELECT last_insert_rowid()
                                  """;

        var sqlParameters = new
        {
            payment.InvoiceId,
            payment.PaymentTypeId,
            payment.Amount,
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

        const string sqlCommand = """
                                  SELECT
                                      PaymentId, 
                                      InvoiceId, 
                                      PaymentTypeId, 
                                      DateCreated, 
                                      Note, 
                                      Amount
                                  FROM Payment 
                                  WHERE InvoiceId = @invoiceId
                                  """;

        var sqlParameters = new
        {
            invoiceId = id
        };

        var results = connection.Query<Payment>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<Payment>();
    }

    public IEnumerable<Payment> GetByDate(DateOnly date)
    {
        return GetByDateRange(date, date);
    }

    public IEnumerable<Payment> GetByDateRange(DateOnly start, DateOnly end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT
                                      PaymentId, 
                                      InvoiceId, 
                                      PaymentTypeId, 
                                      DateCreated, 
                                      Note, 
                                      Amount
                                  FROM Payment 
                                  WHERE DateCreated BETWEEN @startDate AND @endDate
                                  """;

        var sqlParameters = new
        {
            startDate = start.ToStartDateString(),
            endDate = end.ToEndDateString()
        };

        var results = connection.Query<Payment>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<Payment>();
    }

    public IEnumerable<Payment> GetByPaymentTypeId(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT
                                      PaymentId, 
                                      InvoiceId, 
                                      PaymentTypeId, 
                                      DateCreated, 
                                      Note, 
                                      Amount
                                  FROM Payment 
                                  WHERE PaymentTypeId = @PaymentTypeId
                                  """;

        var sqlParameters = new
        {
            PaymentTypeId = id
        };

        var results = connection.Query<Payment>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<Payment>();
    }

	public bool RemoveById(int id)
	{
		return RemoveByIdInternal(id);
	}

	private bool RemoveByIdInternal(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = """
                                  DELETE 
                                  FROM Payment 
                                  WHERE PaymentId = @PaymentId
                                  """;

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

		const string sqlCommand = """
                                  DELETE 
                                  FROM InvoiceProduct 
                                  WHERE InvoiceId = @InvoiceId
                                  """;

		var sqlParameters = new
		{
			InvoiceId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount >= 0;
	}
}