using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using IndyPOS.DataServices.Models;

namespace IndyPOS.DataServices
{
    public class InventoryProductsDataService : SQLiteDatabase, IInventoryProductsDataService
    {
        public InventoryProductModel GetProductByBarcode(string barcode)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var result = connection.QueryFirst<InventoryProductModel>
                    (
                        "SELECT * FROM [InventoryProducts] WHERE [Barcode] = @productBarcode",
                        new { productBarcode = barcode }
                    );

                return result;
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
    }
}
