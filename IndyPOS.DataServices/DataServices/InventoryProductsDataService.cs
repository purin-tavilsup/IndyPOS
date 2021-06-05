using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;

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
    }
}
