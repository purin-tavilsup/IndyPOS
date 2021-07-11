using Dapper;
using IndyPOS.DataServices.Models;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.DataServices
{
    public class InventoryProductsDataService : SQLiteDatabase, IInventoryProductsDataService
    {
        public InventoryProductModel GetProductByBarcode(string barcode)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<InventoryProductModel>
                    (
                        "SELECT * FROM [InventoryProducts] WHERE [Barcode] = @productBarcode",
                        new { productBarcode = barcode }
                    );

                return results.FirstOrDefault();
            }
        }

        public IList<InventoryProductModel> GetProductsByCategoryId(int categoryId)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<InventoryProductModel>
                    (
                        "SELECT * FROM [InventoryProducts] WHERE [Category] = @category",
                        new { category = categoryId }
                    );

                return results.ToList();
            }
        }

        public void AddProduct(InventoryProductModel product)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                //
            }
        }

        public void UpdateProduct(InventoryProductModel product)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                //
            }
        }

        public void RemoveProduct(InventoryProductModel product)
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
    }
}
