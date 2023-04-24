using Dapper;
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
            payment.PaymentId,
            payment.Description,
            payment.InvoiceId,
            ReceivableAmount = payment.ReceivableAmount.ToMoneyString()
        };

        var rowId = connection.Query<int>(sqlCommand, sqlParameters)
                              .FirstOrDefault();

        return rowId;
    }

    public bool Update(PayLaterPayment payment)
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

        const string sqlCommand = @"SELECT * FROM AccountsReceivables";

        var results = connection.Query(sqlCommand);

        return MapPayLaterPayments(results);
    }

    public IEnumerable<PayLaterPayment> GetIncompletePayLaterPayments()
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM AccountsReceivables WHERE IsCompleted = @IsCompleted";

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

        const string sqlCommand = @"SELECT * FROM AccountsReceivables WHERE InvoiceId = @InvoiceId";

        var sqlParameters = new
        {
            InvoiceId = invoiceId
        };

        var result = connection.Query(sqlCommand, sqlParameters)
                               .FirstOrDefault();

        return result is null ? null : MapPayLaterPayment(result);
    }

    public PayLaterPayment? GetById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM AccountsReceivables WHERE PaymentId = @PaymentId";

        var sqlParameters = new
        {
            PaymentId = id
        };

        var result = connection.Query(sqlCommand, sqlParameters)
                               .FirstOrDefault();

        return result is null ? null : MapPayLaterPayment(result);
    }

    public IEnumerable<PayLaterPayment> GetPayLaterPaymentsByDateRange(DateTime start, DateTime end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM AccountsReceivables WHERE DateCreated BETWEEN @startDate AND @endDate";

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

		const string sqlCommand = @"DELETE FROM AccountsReceivables WHERE PaymentId = @PaymentId";

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