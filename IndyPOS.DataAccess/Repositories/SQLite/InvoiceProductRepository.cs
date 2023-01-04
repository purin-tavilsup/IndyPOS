using Dapper;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class InvoiceProductRepository : IInvoiceProductRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public InvoiceProductRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public int AddInvoiceProduct(InvoiceProduct product)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"INSERT INTO InvoiceProducts
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

	public IEnumerable<InvoiceProduct> GetInvoiceProductsByInvoiceId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                InvoiceProductId,
                Priority,
                InvoiceId,
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
                FROM InvoiceProducts 
                WHERE InvoiceId = @invoiceId";

		var sqlParameters = new
		{
			invoiceId = id
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<InvoiceProduct>() : MapInvoiceProducts(results);
	}

	public IEnumerable<InvoiceProduct> GetInvoiceProductsByDateRange(DateTime start, DateTime end)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT
                InvoiceProductId,
                Priority,
                InvoiceId,
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
                FROM InvoiceProducts 
                WHERE DateCreated BETWEEN @startDate AND @endDate";

		var sqlParameters = new
		{
			startDate = start.ToStartDateString(),
			endDate = end.ToEndDateString()
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<InvoiceProduct>() : MapInvoiceProducts(results);
	}

	public IEnumerable<InvoiceProduct> GetInvoiceProductsByDate(DateTime date)
	{
		return GetInvoiceProductsByDateRange(date, date);
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
			UnitPrice = ((string)x.UnitPrice).ToMoney(),
			Quantity = (int)x.Quantity,
			DateCreated = x.DateCreated,
			Note = x.Note
		});

		return products.ToList();
	}
}