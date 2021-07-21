using Dapper;
using IndyPOS.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using IndyPOS.DataAccess.Repositories;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
	public class PackPriceRepository : IPackPriceRepository
	{
        private readonly IDbConnectionProvider _dbConnectionProvider;

        public PackPriceRepository(IDbConnectionProvider dbConnectionProvider)
		{
            _dbConnectionProvider = dbConnectionProvider;
        }

        public IList<PackPrice> GetPackPricesByBarcode(string barcode)
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand = "SELECT * FROM [PackPrices] WHERE [Barcode] = @productBarcode";

                var sqlParameters = new
                {
                    productBarcode = barcode
                };

                var results = connection.Query(sqlCommand, sqlParameters);

                return MapPackPrices(results);
            }
        }

        private IList<PackPrice> MapPackPrices(IEnumerable<dynamic> results)
        {
            var packPrices = results?.Select(x => new PackPrice
            {
                Id = (int)x.Id,

                Barcode = x.Barcode,

                QuantityPerPack = (int)x.QuantityPerPack,

                UnitPricePerPack = MapUnitPricePerPackToDecimal(x.UnitPricePerPack),

                DateCreated = x.DateCreated

            }) ?? Enumerable.Empty<PackPrice>();

            return packPrices.ToList();
        }

        private decimal MapUnitPricePerPackToDecimal(string value)
        {
            if (decimal.TryParse(value.Trim(), out var result))
                return result / 100m;

            return 0m;
        }
    }
}
