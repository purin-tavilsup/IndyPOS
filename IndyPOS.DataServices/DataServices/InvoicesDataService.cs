using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;

namespace IndyPOS.DataServices
{
    public class InvoicesDataService : SQLiteDatabase, IInvoicesDataService
    {
        public IList<InventoryProductModel> GetProductsForSale()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<InventoryProductModel>
                    (
                        @"SELECT * 
                        FROM [InventoryProducts]",
                        new DynamicParameters()
                    );

                return results.ToList();
            }
        }
    }
}
