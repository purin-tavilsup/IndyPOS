using Dapper;
using IndyPOS.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using IndyPOS.DataAccess.Repositories;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
	public class PackPriceRepository : SQLiteDatabase, IPackPriceRepository
	{
        public PackPrice GetProductByBarcode(string barcode)
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<PackPrice>
                    (
                        "SELECT * FROM [PackPrices] WHERE [Barcode] = @productBarcode",
                        new { productBarcode = barcode }
                    );

                return results.FirstOrDefault();
            }
        }
    }
}
