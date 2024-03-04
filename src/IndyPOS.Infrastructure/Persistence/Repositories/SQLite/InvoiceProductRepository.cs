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

        const string sqlCommand = """
                                  INSERT INTO InvoiceProduct
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
                                       Note,
                                       GroupPrice,
                                       IsGroupProduct,
                                       OriginalUnitPrice
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
                                       @Note,
                                       @GroupPrice,
                                       @IsGroupProduct,
                                       @OriginalUnitPrice
                                       );
                                       SELECT last_insert_rowid()
                                  """;

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
            product.UnitPrice,
            product.Quantity,
            product.Note,
            product.GroupPrice,
            product.IsGroupProduct,
            product.OriginalUnitPrice
        };

        var productId = connection.Query<int>(sqlCommand, sqlParameters)
                                  .FirstOrDefault();

        return productId;
    }

    public IEnumerable<InvoiceProduct> GetByInvoiceId(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT
                                      InvoiceProductId,
                                      Priority,
                                      InvoiceId,
                                      InventoryProductId,
                                      Barcode,
                                      Description,
                                      Manufacturer,
                                      Brand,
                                      Category,
                                      Quantity,
                                      IsTrackable,
                                      DateCreated,
                                      Note,
                                      UnitPrice,
                                      GroupPrice,
                                      IsGroupProduct,
                                      OriginalUnitPrice
                                  FROM InvoiceProduct 
                                  WHERE InvoiceId = @invoiceId
                                  """;

        var sqlParameters = new
        {
            invoiceId = id
        };

        var results = connection.Query<InvoiceProduct>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<InvoiceProduct>();
    }

    public IEnumerable<InvoiceProduct> GetByDateRange(DateOnly start, DateOnly end)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT
                                      InvoiceProductId, 
                                      Priority, 
                                      InvoiceId, 
                                      InventoryProductId, 
                                      Barcode, 
                                      Description, 
                                      Manufacturer, 
                                      Brand, 
                                      Category, 
                                      Quantity, 
                                      IsTrackable, 
                                      DateCreated, 
                                      Note, 
                                      UnitPrice, 
                                      GroupPrice, 
                                      IsGroupProduct, 
                                      OriginalUnitPrice
                                  FROM InvoiceProduct 
                                  WHERE DateCreated BETWEEN @startDate AND @endDate
                                  """;

        var sqlParameters = new
        {
            startDate = start.ToStartDateString(),
            endDate = end.ToEndDateString()
        };

        var results = connection.Query<InvoiceProduct>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<InvoiceProduct>();
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

		const string sqlCommand = """
		                          DELETE 
		                          FROM InvoiceProduct 
		                          WHERE InvoiceProductId = @InvoiceProductId
		                          """;

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