using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using IndyPOS.DataAccess.Models;
using IndyPOS.DataAccess.Repositories;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
    public class InvoiceRepository : SQLiteDatabase, IInvoiceRepository
    {
        public IList<InventoryProduct> GetProductsForSale()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<InventoryProduct>
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
