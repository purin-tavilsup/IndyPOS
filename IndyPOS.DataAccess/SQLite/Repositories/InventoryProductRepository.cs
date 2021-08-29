using Dapper;
using IndyPOS.DataAccess.Extensions;
using IndyPOS.DataAccess.Models;
using IndyPOS.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
	public class InventoryProductRepository : IInventoryProductRepository
    {
        private readonly IDbConnectionProvider _dbConnectionProvider;

        public InventoryProductRepository(IDbConnectionProvider dbConnectionProvider)
        {
            _dbConnectionProvider = dbConnectionProvider;
        }

        public InventoryProduct GetProductByBarcode(string barcode)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT * FROM InventoryProducts 
                WHERE Barcode = @productBarcode";

                var sqlParameters = new
                {
                    productBarcode = barcode
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInventoryProducts(results).FirstOrDefault();
            }
        }

        public IList<InventoryProduct> GetProductsByCategoryId(int id)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT * FROM InventoryProducts 
                WHERE Category = @category";

                var sqlParameters = new
                {
                    category = id
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInventoryProducts(results);
            }
        }

        public InventoryProduct GetProductByInventoryProductId(int id)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"SELECT * FROM InventoryProducts 
                WHERE InventoryProductId = @inventoryProductId";

                var sqlParameters = new
                {
                    inventoryProductId = id
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInventoryProducts(results).FirstOrDefault();
            }
        }

        public int AddProduct(InventoryProduct product)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"INSERT INTO InventoryProducts
                (
                    Barcode,
                    Description,
                    Manufacturer,
                    Brand,
                    Category,
                    UnitCost,
                    UnitPrice,
                    QuantityInStock,
                    DateCreated
                )
                VALUES
                (
                    @Barcode,
                    @Description,
                    @Manufacturer,
                    @Brand,
                    @Category,
                    @UnitCost,
                    @UnitPrice,
                    @QuantityInStock,
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
                    UnitCost = MapMoneyToString(product.UnitCost),
                    UnitPrice = MapMoneyToString(product.UnitPrice),
					product.QuantityInStock
                };

                var inventoryProductId = connection.Query<int>(sqlCommand, sqlParameters).FirstOrDefault();

                if (inventoryProductId < 1) throw new Exception("Failed to get the last insert Row ID after adding a product.");

                return inventoryProductId;
            }
        }

        public void UpdateProduct(InventoryProduct product)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = @"UPDATE InventoryProducts
                SET
                    Description = @Description,
                    Manufacturer = @Manufacturer,
                    Brand = @Brand,
                    Category = @Category,
                    UnitCost = @UnitCost,
                    UnitPrice = @UnitPrice,
                    QuantityInStock = @QuantityInStock,
                    DateUpdated = datetime('now','localtime')
                WHERE InventoryProductId = @InventoryProductId";

                var sqlParameters = new
                {
                    product.InventoryProductId,
                    product.Description,
                    product.Manufacturer,
                    product.Brand,
                    product.Category,
                    UnitCost = MapMoneyToString(product.UnitCost),
                    UnitPrice = MapMoneyToString(product.UnitPrice),
                    product.QuantityInStock
                };

                var affectedRowsCount = connection.Execute(sqlCommand, sqlParameters);

                if (affectedRowsCount != 1)
                    throw new Exception("Failed to update the product.");
            }
        }

        public void RemoveProduct(InventoryProduct product)
        {
            RemoveProductByInventoryProductId(product.InventoryProductId);
        }

        public void RemoveProductByInventoryProductId(int id)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
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
        }

        private IList<InventoryProduct> MapInventoryProducts(IEnumerable<dynamic> results)
		{
            var products = results?.Select(x => new InventoryProduct
            {
                InventoryProductId = (int)x.InventoryProductId,

                Barcode = x.Barcode,

                Description = x.Description,

                Manufacturer = x.Manufacturer,

                Brand = x.Brand,

                Category = (int)x.Category,

                UnitCost = MapMoneyToNullableDecimal(x.UnitCost),

                UnitPrice = MapMoneyToDecimal(x.UnitPrice),

                QuantityInStock = (int)x.QuantityInStock,

                DateCreated = x.DateCreated,

                DateUpdated = x.DateUpdated
            }) ?? Enumerable.Empty<InventoryProduct>();

            return products.ToList();
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

        private string MapMoneyToString(decimal? value)
        {
            if (!value.HasValue)
                return null;

            var result = Math.Round(value.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero) * 100m;

            return result.ToString();
        }
    }
}
