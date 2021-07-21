using Dapper;
using System.Collections.Generic;
using System.Linq;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
    public class StoreConstantRepository : SQLiteDatabase, IStoreConstantRepository
    {
        public IList<PaymentType> GetPaymentTypes()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<PaymentType>
                    (
                        @"SELECT * 
                        FROM [PaymentTypes]",
                        new DynamicParameters()
                    );

                return results.ToList();
            }
        }

        public IList<UserRole> GetUserRoles()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<UserRole>
                    (
                        @"SELECT * 
                        FROM [UserRoles]",
                        new DynamicParameters()
                    );

                return results.ToList();
            }
        }

        public IList<ProductCategory> GetProductCategories()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<ProductCategory>
                    (
                        @"SELECT * 
                        FROM [ProductCategories]",
                        new DynamicParameters()
                    );

                return results.ToList();
            }
        }
    }
}
