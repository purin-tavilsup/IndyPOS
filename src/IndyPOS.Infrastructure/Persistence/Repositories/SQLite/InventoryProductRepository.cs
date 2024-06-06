using Dapper;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Domain.Entities;

namespace IndyPOS.Infrastructure.Persistence.Repositories.SQLite;

public class InventoryProductRepository : IInventoryProductRepository
{
    private readonly IDbConnectionProvider _dbConnectionProvider;

    public InventoryProductRepository(IDbConnectionProvider dbConnectionProvider)
    {
        _dbConnectionProvider = dbConnectionProvider;
    }

    public InventoryProduct GetByBarcode(string barcode)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      InventoryProductId, 
                                      Barcode, 
                                      Description, 
                                      Manufacturer, 
                                      Brand, 
                                      Category, 
                                      QuantityInStock, 
                                      GroupPriceQuantity, 
                                      IsTrackable, 
                                      DateCreated, 
                                      DateUpdated, 
                                      UnitPrice, 
                                      GroupPrice
                                  FROM InventoryProduct 
                                  WHERE Barcode = @productBarcode
                                  """;

        var sqlParameters = new
        {
            productBarcode = barcode
        };

        var result = connection.Query<InventoryProduct>(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new ProductNotFoundException($"Could not find Inventory Product by Barcode: {barcode}");
		}

        return result;
    }

    public IEnumerable<InventoryProduct> GetProductsByCategoryId(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      InventoryProductId, 
                                      Barcode, 
                                      Description, 
                                      Manufacturer, 
                                      Brand, 
                                      Category, 
                                      QuantityInStock, 
                                      GroupPriceQuantity, 
                                      IsTrackable, 
                                      DateCreated, 
                                      DateUpdated, 
                                      UnitPrice, 
                                      GroupPrice
                                  FROM InventoryProduct 
                                  WHERE Category = @category
                                  """;

        var sqlParameters = new
        {
            category = id
        };

        var results = connection.Query<InventoryProduct>(sqlCommand, sqlParameters);

        return results ?? Enumerable.Empty<InventoryProduct>();
    }

    public InventoryProduct GetById(int id)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      InventoryProductId, 
                                      Barcode, 
                                      Description, 
                                      Manufacturer, 
                                      Brand, 
                                      Category, 
                                      QuantityInStock, 
                                      GroupPriceQuantity, 
                                      IsTrackable, 
                                      DateCreated, 
                                      DateUpdated, 
                                      UnitPrice, 
                                      GroupPrice
                                  FROM InventoryProduct 
                                  WHERE InventoryProductId = @inventoryProductId
                                  """;

        var sqlParameters = new
        {
            inventoryProductId = id
        };

        var result = connection.Query<InventoryProduct>(sqlCommand, sqlParameters)
                               .FirstOrDefault();

		if (result is null)
		{
			throw new ProductNotFoundException($"Could not find Inventory Product by ID: {id}");
		}

        return result;
    }

    public int Add(InventoryProduct product)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  INSERT INTO InventoryProduct
                                      (Barcode,
                                       Description,
                                       Manufacturer,
                                       Brand,
                                       Category,
                                       UnitPrice,
                                       QuantityInStock,
                                       GroupPrice,
                                       GroupPriceQuantity,
                                       IsTrackable,
                                       DateCreated)
                                  VALUES
                                      (@Barcode,
                                       @Description,
                                       @Manufacturer,
                                       @Brand,
                                       @Category,
                                       @UnitPrice,
                                       @QuantityInStock,
                                       @GroupPrice,
                                       @GroupPriceQuantity,
                                       @IsTrackable,
                                       datetime('now','localtime'));
                                  SELECT last_insert_rowid()
                                  """;

        var sqlParameters = new
        {
            product.Barcode,
            product.Description,
            product.Manufacturer,
            product.Brand,
            product.Category,
            product.UnitPrice,
            product.QuantityInStock,
            product.GroupPrice,
            product.GroupPriceQuantity,
            IsTrackable = product.IsTrackable ? 1 : 0
        };

        var productId = connection.Query<int>(sqlCommand, sqlParameters)
                                  .FirstOrDefault();

        return productId;
    }

    public bool Update(InventoryProduct product)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  UPDATE InventoryProduct
                                  SET
                                      Description = @Description,
                                      Manufacturer = @Manufacturer,
                                      Brand = @Brand,
                                      Category = @Category,
                                      UnitPrice = @UnitPrice,
                                      QuantityInStock = @QuantityInStock,
                                      GroupPrice = @GroupPrice,
                                      GroupPriceQuantity = @GroupPriceQuantity,
                                      DateUpdated = datetime('now','localtime')
                                  WHERE InventoryProductId = @InventoryProductId
                                  """;

        var sqlParameters = new
        {
            product.InventoryProductId,
            product.Description,
            product.Manufacturer,
            product.Brand,
            product.Category,
            product.UnitPrice,
            product.QuantityInStock,
            product.GroupPrice,
            product.GroupPriceQuantity
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }

    public bool UpdateProductQuantityById(int id, int quantity)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  UPDATE InventoryProduct
                                  SET
                                      QuantityInStock = @QuantityInStock
                                  WHERE InventoryProductId = @InventoryProductId
                                  """;

        var sqlParameters = new
        {
            InventoryProductId = id,
            QuantityInStock = quantity
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }

    public bool Remove(InventoryProduct product)
    {
        return RemoveById(product.InventoryProductId);
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
                                  FROM InventoryProduct 
                                  WHERE InventoryProductId = @InventoryProductId
                                  """;

		var sqlParameters = new
		{
			InventoryProductId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		return affectedRowsCount == 1;
    }

    public int GetProductBarcodeCounter()
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  SELECT 
                                      Id,
                                      Counter
                                  FROM ProductBarcodeCounter 
                                  WHERE Id = @Id
                                  """;

        var sqlParameters = new
        {
            Id = 1
        };

        var results = connection.Query(sqlCommand, sqlParameters);

        var result = results.FirstOrDefault();

        return result != null ? (int)result.Counter : 1;
    }

    public bool UpdateProductBarcodeCounter(int counter)
    {
        using var connection = _dbConnectionProvider.GetDbConnection();
        connection.Open();

        const string sqlCommand = """
                                  UPDATE ProductBarcodeCounter
                                  SET
                                      Counter = @Counter
                                  WHERE Id = @Id
                                  """;

        var sqlParameters = new
        {
            Id = 1,
            Counter = counter
        };

        var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

        return affectedRowsCount == 1;
    }

	public IEnumerable<InventoryProduct> GetProductsByDescriptionKeyword(string keyword)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = """
                                  SELECT
                                      InventoryProductId, 
                                      Barcode, 
                                      Description, 
                                      Manufacturer, 
                                      Brand, 
                                      Category, 
                                      QuantityInStock, 
                                      GroupPriceQuantity, 
                                      IsTrackable, 
                                      DateCreated, 
                                      DateUpdated, 
                                      UnitPrice, 
                                      GroupPrice
                                  FROM InventoryProduct 
                                  WHERE Description LIKE @Keyword
                                  """;

		var sqlParameters = new
		{
			Keyword = $"%{keyword}%"
		};

		var results = connection.Query<InventoryProduct>(sqlCommand, sqlParameters);

		return results ?? Enumerable.Empty<InventoryProduct>();
	} 

    public IEnumerable<InventoryProduct> GetProductsByBrandKeyword(string keyword)
    {
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = """
                                  SELECT
                                      InventoryProductId, 
                                      Barcode, 
                                      Description, 
                                      Manufacturer, 
                                      Brand, 
                                      Category, 
                                      QuantityInStock, 
                                      GroupPriceQuantity, 
                                      IsTrackable, 
                                      DateCreated, 
                                      DateUpdated, 
                                      UnitPrice, 
                                      GroupPrice
                                  FROM InventoryProduct 
                                  WHERE Brand LIKE @Keyword
                                  """;

		var sqlParameters = new
		{
			Keyword = $"%{keyword}%"
		};

		var results = connection.Query<InventoryProduct>(sqlCommand, sqlParameters);

		return results ?? Enumerable.Empty<InventoryProduct>();
    }
}