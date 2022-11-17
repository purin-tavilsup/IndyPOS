#nullable enable
using Dapper;
using IndyPOS.Common.Exceptions;
using IndyPOS.Common.Extensions;
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

		var results = connection.Query(sqlCommand, sqlParameters);
		var product = MapInventoryProducts(results).FirstOrDefault();

		if (product == null)
			throw new ProductNotFoundException($"Product with barcode {barcode} is not found.");

		return product;
	}

	public IList<InventoryProduct> GetProductsByCategoryId(int id)
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

		return MapInventoryProducts(results).ToList();
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

		var results = connection.Query(sqlCommand, sqlParameters);
		var product = MapInventoryProducts(results).FirstOrDefault();

		if (product == null)
			throw new ProductNotFoundException($"Product with Id {id} is not found.");

		return product;
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
			UnitPrice = MapMoneyToString(product.UnitPrice),
			product.QuantityInStock,
			GroupPrice = MapMoneyToString(product.GroupPrice),
			product.GroupPriceQuantity,
			IsTrackable = product.IsTrackable ? 1 : 0
		};

		var inventoryProductId = connection.Query<int>(sqlCommand, sqlParameters).FirstOrDefault();

		if (inventoryProductId < 1) throw new Exception("Failed to get the last insert Row ID after adding a product.");

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
			UnitPrice = MapMoneyToString(product.UnitPrice),
			product.QuantityInStock,
			GroupPrice = MapMoneyToString(product.GroupPrice),
			product.GroupPriceQuantity
		};

		var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

		if (affectedRowsCount != 1)
			throw new Exception("Failed to update the product.");
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
			throw new Exception("Failed to update product's quantity.");
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
			throw new Exception("Failed to delete the product.");
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

	private IEnumerable<InventoryProduct> MapInventoryProducts(IEnumerable<dynamic>? results)
	{
		if (results is null)
			return Enumerable.Empty<InventoryProduct>();

		var products = results.Select(x => new InventoryProduct
		{
			InventoryProductId = (int)x.InventoryProductId,

			Barcode = x.Barcode,

			Description = x.Description,

			Manufacturer = x.Manufacturer,

			Brand = x.Brand,

			Category = (int)x.Category,

			UnitPrice = MapMoneyToDecimal(x.UnitPrice),

			QuantityInStock = (int)x.QuantityInStock,

			GroupPrice = MapMoneyToNullableDecimal(x.GroupPrice),

			GroupPriceQuantity = (int?)x.GroupPriceQuantity,

			IsTrackable = x.IsTrackable == 1,

			DateCreated = x.DateCreated,

			DateUpdated = x.DateUpdated
		});

		return products;
	}

	private decimal? MapMoneyToNullableDecimal(string value)
	{
		if (!value.HasValue())
			return null;

		if (decimal.TryParse(value.Trim(), out var result))
			return result / 100m;

		return null;
	}

	private decimal MapMoneyToDecimal(string value)
	{
		if (decimal.TryParse(value.Trim(), out var result))
			return result / 100m;

		return 0m;
	}

	private string? MapMoneyToString(decimal? value)
	{
		if (!value.HasValue)
			return null;

		var result = Math.Round(value.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero) * 100m;

		return $"{result}";
	}
}