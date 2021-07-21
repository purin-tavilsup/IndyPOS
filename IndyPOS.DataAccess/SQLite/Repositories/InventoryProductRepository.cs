using Dapper;
using IndyPOS.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.DataAccess.Extensions;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
    public class InventoryProductRepository : SQLiteDatabase, IInventoryProductRepository
    {
        public InventoryProduct GetProductByBarcode(string barcode)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                const string sqlCommand = "SELECT * FROM [InventoryProducts] WHERE [Barcode] = @productBarcode";

                var sqlParameters = new
                {
                    productBarcode = barcode
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInventoryProducts(results).FirstOrDefault();
            }
        }

        public IList<InventoryProduct> GetProductsByCategoryId(int categoryId)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                const string sqlCommand = "SELECT * FROM [InventoryProducts] WHERE [Category] = @category";

                var sqlParameters = new
                {
                    category = categoryId
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapInventoryProducts(results);
            }
        }

        public void AddProduct(InventoryProduct product)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                //
            }
        }

        public void UpdateProduct(InventoryProduct product)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                //
            }
        }

        public void RemoveProduct(InventoryProduct product)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                //
            }
        }

        public void RemoveProductByBarcode(string barcode)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                //
            }
        }

        private IList<InventoryProduct> MapInventoryProducts(IEnumerable<dynamic> results)
		{
            var products = results?.Select(x => new InventoryProduct
            {
                InventoryProductId = (int)x.InventoryProductId,

                SKU = x.SKU,

                Barcode = x.Barcode,

                Description = x.Description,

                Manufacturer = x.Manufacturer,

                Brand = x.Brand,

                Category = (int)x.Category,

                UnitCost = MapUnitCostToDecimal(x.UnitCost),

                UnitPrice = MapUnitPriceToDecimal(x.UnitPrice),

                QuantityInStock = (int)x.QuantityInStock,

                DateCreated = x.DateCreated,

                DateUpdated = x.DateUpdated
            }) ?? Enumerable.Empty<InventoryProduct>();

            return products.ToList();
        }

        private decimal? MapUnitCostToDecimal(string value)
		{
            if (!value.HasValue())
                return null;

            if (decimal.TryParse(value.Trim(), out var result))
                return result / 100m;

            return null;
        }

        private decimal MapUnitPriceToDecimal(string value)
        {
            if (decimal.TryParse(value.Trim(), out var result))
                return result / 100m;

            return 0m;
        }
    }
}
