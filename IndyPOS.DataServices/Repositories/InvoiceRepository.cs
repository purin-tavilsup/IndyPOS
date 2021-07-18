using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories
{
    public class InvoiceRepository : SQLiteDatabase, IInvoiceRepository
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
