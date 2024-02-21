using Dapper;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
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

        const string sqlCommand = @"INSERT INTO PayLater
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
                SELECT last_insert_rowid()";

        var sqlParameters = new
        {
            payment.PaymentId,
            payment.Description,
            payment.InvoiceId,
            PayLaterAmount = payment.PayLaterAmount.ToMoneyString()
        };

        var rowId = connection.Query<int>(sqlCommand, sqlParameters)
                              .FirstOrDefault();

        return rowId;
    }

    public bool Update(PayLaterPayment payment)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"UPDATE PayLater
                SET
                    PaidAmount = @PaidAmount,
                    IsCompleted = @IsCompleted,
                    DateUpdated = datetime('now','localtime')
                WHERE PaymentId = @PaymentId";

        var sqlParameters = new
        {
            payment.PaymentId,
            PaidAmount = payment.PaidAmount.ToMoneyString(),
            IsCompleted = payment.IsCompleted ? 1 : 0
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }

    public IEnumerable<PayLaterPayment> GetAll()
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM PayLater";

        var results = connection.Query(sqlCommand);

        return MapPayLaterPayments(results);
    }

    public IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments()
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM PayLater WHERE IsCompleted = @IsCompleted";

        var sqlParameters = new
        {
            IsCompleted = 0
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<PayLaterPayment>() : MapPayLaterPayments(results);
    }

    public IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDescriptionKeyword(string keyword)
    {
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM PayLater WHERE Description LIKE @Keyword";

		var sqlParameters = new
		{
			Keyword = $"%{keyword}%"
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<PayLaterPayment>() : MapPayLaterPayments(results);
    }

    public PayLaterPayment GetPayLaterPaymentByInvoiceId(int invoiceId)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM PayLater WHERE InvoiceId = @InvoiceId";

        var sqlParameters = new
        {
            InvoiceId = invoiceId
        };

        var result = connection.Query(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new PayLaterPaymentNotFoundException($"Could not find Pay Later Payment by Invoice ID: {invoiceId}");
		}

        return MapPayLaterPayment(result);
    }

    public PayLaterPayment GetById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM PayLater WHERE PaymentId = @PaymentId";

        var sqlParameters = new
        {
            PaymentId = id
        };

        var result = connection.Query(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new PayLaterPaymentNotFoundException($"Could not find Pay Later Payment by ID: {id}");
		}

        return MapPayLaterPayment(result);
    }

    public IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateOnly start, DateOnly end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM PayLater WHERE DateCreated BETWEEN @startDate AND @endDate";

        var sqlParameters = new
        {
            startDate = start.ToStartDateString(),
            endDate = end.ToEndDateString()
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<PayLaterPayment>() : MapPayLaterPayments(results);
    }

	public bool RemoveById(int id)
	{
		return RemoveByIdInternal(id);
	}

	private bool RemoveByIdInternal(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"DELETE FROM PayLater WHERE PaymentId = @PaymentId";

		var sqlParameters = new
		{
			PaymentId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount == 1;
	}

    private static PayLaterPayment MapPayLaterPayment(dynamic result)
    {
        var payment = new PayLaterPayment
        {
            PaymentId = (int)result.PaymentId,
            Description = result.Description,
            InvoiceId = (int)result.InvoiceId,
            PayLaterAmount = (decimal)result.PayLaterAmount,
            PaidAmount = (decimal)result.PaidAmount,
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