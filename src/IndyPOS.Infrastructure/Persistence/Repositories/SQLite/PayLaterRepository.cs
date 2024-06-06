using Dapper;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Domain.Entities;
using IndyPOS.Infrastructure.Extensions;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class PayLaterRepository : IPayLaterPaymentRepository
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    public PayLaterRepository(IDbConnectionProvider dbConnectionProvider)
    {
        _dbConnectionProvider = dbConnectionProvider;
    }

    public int Add(PayLaterPayment payment)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  INSERT INTO PayLater
                                                  (
                                                   PaymentId,
                                                   Description,
                                                   InvoiceId,
                                                   PayLaterAmount,
                                                   DateCreated
                                                  )
                                                  VALUES
                                                  (
                                                   @PaymentId,
                                                   @Description,
                                                   @InvoiceId,
                                                   @PayLaterAmount,
                                                   datetime('now','localtime')
                                                  );
                                                  SELECT last_insert_rowid()
                                  """;

        var sqlParameters = new
        {
            payment.PaymentId,
            payment.Description,
            payment.InvoiceId,
            payment.PayLaterAmount
        };

        var rowId = connection.Query<int>(sqlCommand, sqlParameters)
                              .FirstOrDefault();

        return rowId;
    }

    public bool Update(PayLaterPayment payment)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  UPDATE PayLater
                                  SET
                                  PaidAmount = @PaidAmount,
                                  IsCompleted = @IsCompleted,
                                  DateUpdated = datetime('now','localtime')
                                  WHERE PaymentId = @PaymentId
                                  """;

        var sqlParameters = new
        {
            payment.PaymentId,
            payment.PaidAmount,
            IsCompleted = payment.IsCompleted ? 1 : 0
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }

    public IEnumerable<PayLaterPayment> GetAll()
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      PaymentId, 
                                      Description, 
                                      InvoiceId, 
                                      IsCompleted, 
                                      DateCreated, 
                                      DateUpdated, 
                                      PayLaterAmount, 
                                      PaidAmount
                                  FROM PayLater
                                  """;

        var results = connection.Query<PayLaterPayment>(sqlCommand);

        return results ?? Enumerable.Empty<PayLaterPayment>();
    }

    public IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments()
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      PaymentId,
                                      Description,
                                      InvoiceId,
                                      IsCompleted,
                                      DateCreated,
                                      DateUpdated,
                                      PayLaterAmount,
                                      PaidAmount
                                  FROM PayLater
                                  WHERE IsCompleted = @IsCompleted
                                  """;

        var sqlParameters = new
        {
            IsCompleted = 0
        };

        var results = connection.Query<PayLaterPayment>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<PayLaterPayment>();
    }

    public IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDescriptionKeyword(string keyword)
    {
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = """
                                  SELECT
                                      PaymentId,
                                      Description,
                                      InvoiceId,
                                      IsCompleted,
                                      DateCreated,
                                      DateUpdated,
                                      PayLaterAmount,
                                      PaidAmount
                                  FROM PayLater 
                                  WHERE Description LIKE @Keyword
                                  """;

		var sqlParameters = new
		{
			Keyword = $"%{keyword}%"
		};

		var results = connection.Query<PayLaterPayment>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<PayLaterPayment>();
    }

    public PayLaterPayment? GetPayLaterPaymentByInvoiceId(int invoiceId)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      PaymentId,
                                      Description,
                                      InvoiceId,
                                      IsCompleted,
                                      DateCreated,
                                      DateUpdated,
                                      PayLaterAmount,
                                      PaidAmount
                                  FROM PayLater 
                                  WHERE InvoiceId = @InvoiceId
                                  """;

        var sqlParameters = new
        {
            InvoiceId = invoiceId
        };

        var result = connection.Query<PayLaterPayment>(sqlCommand, sqlParameters)
                               .FirstOrDefault();

        return result;
    }

    public PayLaterPayment GetById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      PaymentId,
                                      Description,
                                      InvoiceId,
                                      IsCompleted,
                                      DateCreated,
                                      DateUpdated,
                                      PayLaterAmount,
                                      PaidAmount
                                  FROM PayLater 
                                  WHERE PaymentId = @PaymentId
                                  """;

        var sqlParameters = new
        {
            PaymentId = id
        };

        var result = connection.Query<PayLaterPayment>(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new PayLaterPaymentNotFoundException($"Could not find Pay Later Payment by ID: {id}");
		}

        return result;
    }

    public IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateOnly start, DateOnly end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT
                                      PaymentId,
                                      Description,
                                      InvoiceId,
                                      IsCompleted,
                                      DateCreated,
                                      DateUpdated,
                                      PayLaterAmount,
                                      PaidAmount
                                  FROM PayLater 
                                  WHERE DateCreated BETWEEN @startDate AND @endDate
                                  """;

        var sqlParameters = new
        {
            startDate = start.ToStartDateString(),
            endDate = end.ToEndDateString()
        };

        var results = connection.Query<PayLaterPayment>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<PayLaterPayment>();
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
                                  FROM PayLater 
                                  WHERE PaymentId = @PaymentId
                                  """;

		var sqlParameters = new
		{
			PaymentId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount == 1;
	}
}