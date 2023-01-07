using Dapper;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using IndyPOS.Infrastructure.Extensions;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    public InvoiceRepository(IDbConnectionProvider dbConnectionProvider)
    {
        _dbConnectionProvider = dbConnectionProvider;
    }

    public int Add(Invoice invoice)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"INSERT INTO Invoices
                (
                    Total,
                    CustomerId,
                    UserId,
                    DateCreated
                )
                VALUES
                (
                    @Total,
                    @CustomerId,
                    @UserId,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

        var sqlParameters = new
        {
            Total = invoice.Total.ToMoneyString(),
            invoice.CustomerId,
            invoice.UserId
        };

        var invoiceId = connection.Query<int>(sqlCommand, sqlParameters)
                                  .FirstOrDefault();

        return invoiceId;
    }

    public Invoice? GetById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT
                InvoiceId,
                Total,
                CustomerId,
                UserId,
                DateCreated
                FROM Invoices 
                WHERE InvoiceId = @invoiceId";

        var sqlParameters = new
        {
            invoiceId = id
        };

        var result = connection.Query(sqlCommand, sqlParameters)
                               .FirstOrDefault();

        return result is null ? null : MapInvoice(result);
    }

    public IEnumerable<Invoice> GetByDateRange(DateTime start, DateTime end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT
                InvoiceId,
                Total,
                CustomerId,
                UserId,
                DateCreated
                FROM Invoices 
                WHERE DateCreated BETWEEN @startDate AND @endDate";

        var sqlParameters = new
        {
            startDate = start.ToStartDateString(),
            endDate = end.ToEndDateString()
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<Invoice>() : MapInvoices(results);
    }

    public IEnumerable<Invoice> GetByDate(DateTime date)
    {
        return GetByDateRange(date, date);
    }

    private static Invoice MapInvoice(dynamic result)
    {
        var invoice = new Invoice
        {
            InvoiceId = (int)result.InvoiceId,
            Total = ((string)result.Total).ToMoney(),
            CustomerId = (int?)result.CustomerId,
            UserId = (int)result.UserId,
            DateCreated = result.DateCreated
        };

        return invoice;
    }

    private static IEnumerable<Invoice> MapInvoices(IEnumerable<dynamic> results)
    {
        return results.Select(MapInvoice);
    }
}