using Dapper;
using IndyPOS.Common.Exceptions;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class InvoiceRepository : IInvoiceRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public InvoiceRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public int AddInvoice(Invoice invoice)
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

		if (invoiceId < 1) 
			throw new InvoiceNotAddedException("Failed to add a new invoice.");

		return invoiceId;
	}

	public Invoice GetInvoiceByInvoiceId(int id)
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
		if (result is null)
			throw new InvoiceNotFoundException($"Invoice is not found. InvoiceId: {id}.");

		return MapInvoice(result);
	}

	public IList<Invoice> GetInvoicesByDateRange(DateTime start, DateTime end)
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

		return results is null ? new List<Invoice>() : MapInvoices(results);
	}

	public IList<Invoice> GetInvoicesByDate(DateTime date)
	{
		return GetInvoicesByDateRange(date, date);
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

	private static IList<Invoice> MapInvoices(IEnumerable<dynamic> results)
	{
		var invoices = results.Select(MapInvoice);

		return invoices.ToList();
	}
}