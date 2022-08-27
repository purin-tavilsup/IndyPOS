using System.Collections.Generic;
using System.Linq;
using Dapper;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Repositories.SQLite
{
    public class StoreConstantRepository : IStoreConstantRepository
    {
        private readonly IDbConnectionProvider _dbConnectionProvider;

        public StoreConstantRepository(IDbConnectionProvider dbConnectionProvider)
        {
            _dbConnectionProvider = dbConnectionProvider;
        }

        public IList<PaymentType> GetPaymentTypes()
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand =   @"SELECT * 
                                            FROM [PaymentTypes]";

                var results = connection.Query(sqlCommand, new DynamicParameters());

                return MapPaymentTypes(results);
            }
        }

        public IList<UserRole> GetUserRoles()
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand =   @"SELECT * 
                                            FROM [UserRoles]";

                var results = connection.Query(sqlCommand, new DynamicParameters());

                return MapUserRoles(results);
            }
        }

        public IList<ProductCategory> GetProductCategories()
        {
            using (var connection = _dbConnectionProvider.GetDbConnection())
            {
                connection.Open();

                const string sqlCommand =   @"SELECT * 
                                            FROM [ProductCategories]";

                var results = connection.Query(sqlCommand, new DynamicParameters());

                return MapProductCategories(results);
            }
        }

        private IList<PaymentType> MapPaymentTypes(IEnumerable<dynamic> results)
        {
            var types = results?.Select(x => new PaymentType
            {
                Id = (int)x.Id,

                Type = x.Type
            }) ?? Enumerable.Empty<PaymentType>();

            return types.ToList();
        }

        private IList<UserRole> MapUserRoles(IEnumerable<dynamic> results)
        {
            var roles = results?.Select(x => new UserRole
            {
                Id = (int)x.Id,

                Role = x.Role
            }) ?? Enumerable.Empty<UserRole>();

            return roles.ToList();
        }

        private IList<ProductCategory> MapProductCategories(IEnumerable<dynamic> results)
        {
            var categories = results?.Select(x => new ProductCategory
            {
                Id = (int)x.Id,

                Category = x.Category
            }) ?? Enumerable.Empty<ProductCategory>();

            return categories.ToList();
        }
    }
}
