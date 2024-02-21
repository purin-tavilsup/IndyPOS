using Dapper;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities;
using IndyPOS.Infrastructure.Extensions;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class InvoiceProductRepository : IInvoiceProductRepository
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    public InvoiceProductRepository(IDbConnectionProvider dbConnectionProvider)
    {
        _dbConnectionProvider = dbConnectionProvider;
    }

    public int Add(InvoiceProduct product)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"INSERT INTO InvoiceProduct
                (
                    InvoiceId,
                    Priority,
                    InventoryProductId,
                    Barcode,
                    Description,
                    Manufacturer,
                    Brand,
                    Category,
                    UnitPrice,
                    Quantity,
                    DateCreated,
					Note
                )
                VALUES
                (
                    @InvoiceId,
                    @Priority,
                    @InventoryProductId,
                    @Barcode,
					@Description,
                    @Manufacturer,
                    @Brand,
                    @Category,
                    @UnitPrice,
                    @Quantity,
                    datetime('now','localtime'),
					@Note
                );
                SELECT last_insert_rowid()";

        var sqlParameters = new
        {
            product.InvoiceId,
            product.Priority,
            product.InventoryProductId,
            product.Barcode,
            product.Description,
            product.Manufacturer,
            product.Brand,
            product.Category,
            UnitPrice = product.UnitPrice.ToMoneyString(),
            product.Quantity,
            product.Note
        };

        var productId = connection.Query<int>(sqlCommand, sqlParameters)
                                  .FirstOrDefault();

        return productId;
    }

    public IEnumerable<InvoiceProduct> GetByInvoiceId(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM InvoiceProduct WHERE InvoiceId = @invoiceId";

        var sqlParameters = new
        {
            invoiceId = id
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<InvoiceProduct>() : MapInvoiceProducts(results);
    }

    public IEnumerable<InvoiceProduct> GetByDateRange(DateOnly start, DateOnly end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = @"SELECT * FROM InvoiceProduct WHERE DateCreated BETWEEN @startDate AND @endDate";

        var sqlParameters = new
        {
            startDate = start.ToStartDateString(),
            endDate = end.ToEndDateString()
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        return results is null ? Enumerable.Empty<InvoiceProduct>() : MapInvoiceProducts(results);
    }

    public IEnumerable<InvoiceProduct> GetByDate(DateOnly date)
    {
        return GetByDateRange(date, date);
    }

	public bool RemoveById(int id)
	{
		return RemoveByIdInternal(id);
	}

	private bool RemoveByIdInternal(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"DELETE FROM InvoiceProduct WHERE InvoiceProductId = @InvoiceProductId";

		var sqlParameters = new
		{
			InvoiceProductId = id
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

		const string sqlCommand = @"DELETE FROM InvoiceProduct WHERE InvoiceId = @InvoiceId";

		var sqlParameters = new
		{
			InvoiceId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount >= 0;
	}

    private static IEnumerable<InvoiceProduct> MapInvoiceProducts(IEnumerable<dynamic> results)
    {
        var products = results.Select(x => new InvoiceProduct
        {
            InvoiceProductId = (int)x.InvoiceProductId,
            Priority = (int)x.Priority,
            InvoiceId = (int)x.InvoiceId,
            InventoryProductId = (int)x.InventoryProductId,
            Barcode = x.Barcode,
            Description = x.Description,
            Manufacturer = x.Manufacturer,
            Brand = x.Brand,
            Category = (int)x.Category,
            UnitPrice = (decimal)x.UnitPrice,
            Quantity = (int)x.Quantity,
            DateCreated = x.DateCreated,
            Note = x.Note
        });

        return products.ToList();
    }
}