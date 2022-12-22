#nullable enable
using Dapper;
using IndyPOS.Common.Exceptions;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite;

public class InventoryProductRepository : IInventoryProductRepository
{
	private readonly IDbConnectionProvider _dbConnectionProvider;

	public InventoryProductRepository(IDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public InventoryProduct GetProductByBarcode(string barcode)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM InventoryProducts 
                WHERE Barcode = @productBarcode";

		var sqlParameters = new
		{
			productBarcode = barcode
		};

		var result = connection.Query(sqlCommand, sqlParameters)
							   .FirstOrDefault();

		if (result is null)
			throw new ProductNotFoundException($"Inventory Product is not found. Product barcode: {barcode}.");

		return MapInventoryProduct(result);
	}

	public IEnumerable<InventoryProduct> GetProductsByCategoryId(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM InventoryProducts 
                WHERE Category = @category";

		var sqlParameters = new
		{
			category = id
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		return results is null ? Enumerable.Empty<InventoryProduct>() : MapInventoryProducts(results);
	}

	public InventoryProduct GetProductById(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM InventoryProducts 
                WHERE InventoryProductId = @inventoryProductId";

		var sqlParameters = new
		{
			inventoryProductId = id
		};

		var result = connection.Query(sqlCommand, sqlParameters)
							   .FirstOrDefault();

		if (result is null)
			throw new ProductNotFoundException($"Inventory Product is not found. InventoryProductId: {id}.");

		return MapInventoryProduct(result);
	}

	public int AddProduct(InventoryProduct product)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"INSERT INTO InventoryProducts
                (
                    Barcode,
                    Description,
                    Manufacturer,
                    Brand,
                    Category,
                    UnitPrice,
                    QuantityInStock,
                    GroupPrice,
                    GroupPriceQuantity,
					IsTrackable,
                    DateCreated
                )
                VALUES
                (
                    @Barcode,
                    @Description,
                    @Manufacturer,
                    @Brand,
                    @Category,
                    @UnitPrice,
                    @QuantityInStock,
                    @GroupPrice,
                    @GroupPriceQuantity,
					@IsTrackable,
                    datetime('now','localtime')
                );
                SELECT last_insert_rowid()";

		var sqlParameters = new
		{
			product.Barcode,
			product.Description,
			product.Manufacturer,
			product.Brand,
			product.Category,
			UnitPrice = product.UnitPrice.ToMoneyString(),
			product.QuantityInStock,
			GroupPrice = product.GroupPrice.ToNullableMoneyString(),
			product.GroupPriceQuantity,
			IsTrackable = product.IsTrackable ? 1 : 0
		};

		var inventoryProductId = connection.Query<int>(sqlCommand, sqlParameters)
										   .FirstOrDefault();

		if (inventoryProductId < 1) 
			throw new ProductNotAddedException($"Failed to add an inventory product. Product barcode: {product.Barcode}.");

		return inventoryProductId;
	}

	public void UpdateProduct(InventoryProduct product)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"UPDATE InventoryProducts
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
                WHERE InventoryProductId = @InventoryProductId";

		var sqlParameters = new
		{
			product.InventoryProductId,
			product.Description,
			product.Manufacturer,
			product.Brand,
			product.Category,
			UnitPrice = product.UnitPrice.ToMoneyString(),
			product.QuantityInStock,
			GroupPrice = product.GroupPrice.ToNullableMoneyString(),
			product.GroupPriceQuantity
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new ProductNotUpdatedException($"Failed to update inventory product. InventoryProductId: {product.InventoryProductId}.");
	}

	public void UpdateProductQuantityById(int id, int quantity)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"UPDATE InventoryProducts
                SET
                    QuantityInStock = @QuantityInStock
                WHERE InventoryProductId = @InventoryProductId";

		var sqlParameters = new
		{
			InventoryProductId = id,
			QuantityInStock = quantity
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new ProductNotUpdatedException($"Failed to update inventory product's quantity. InventoryProductId: {id}.");
	}

	public void RemoveProduct(InventoryProduct product)
	{
		RemoveProductById(product.InventoryProductId);
	}

	public void RemoveProductById(int id)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"DELETE FROM InventoryProducts
                WHERE InventoryProductId = @InventoryProductId";

		var sqlParameters = new
		{
			InventoryProductId = id
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new ProductNotDeletedException($"Failed to delete an inventory product. InventoryProductId: {id}.");
	}

	public int GetProductBarcodeCounter()
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"SELECT * FROM ProductBarcodeCounter 
                WHERE Id = @Id";

		var sqlParameters = new
		{
			Id = 1
		};

		var results = connection.Query(sqlCommand, sqlParameters);

		var result = results.FirstOrDefault();

		return result != null ?  (int) result.Counter : 1;
	}

	public void UpdateProductBarcodeCounter(int counter)
	{
		using var connection = _dbConnectionProvider.GetDbConnection();
		connection.Open();

		const string sqlCommand = @"UPDATE ProductBarcodeCounter
                SET
                    Counter = @Counter
                WHERE Id = @Id";

		var sqlParameters = new
		{
			Id = 1,
			Counter = counter
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new Exception("Failed to update product barcode counter.");
	}

	private static InventoryProduct MapInventoryProduct(dynamic result)
	{
		var product = new InventoryProduct
		{
			InventoryProductId = (int)result.InventoryProductId,
			Barcode = result.Barcode,
			Description = result.Description,
			Manufacturer = result.Manufacturer,
			Brand = result.Brand,
			Category = (int)result.Category,
			UnitPrice = ((string)result.UnitPrice).ToMoney(),
			QuantityInStock = (int)result.QuantityInStock,
			GroupPrice = ((string)result.GroupPrice).ToNullableMoney(),
			GroupPriceQuantity = (int?)result.GroupPriceQuantity,
			IsTrackable = result.IsTrackable == 1,
			DateCreated = result.DateCreated,
			DateUpdated = result.DateUpdated
		};

		return product;
	}

	private static IEnumerable<InventoryProduct> MapInventoryProducts(IEnumerable<dynamic> results)
	{
		return results.Select(MapInventoryProduct);
	}
}