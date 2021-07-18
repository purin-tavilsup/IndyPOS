using Dapper;
using IndyPOS.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.DataAccess.Repositories
{
    public class StoreConstantRepository : SQLiteDatabase, IStoreConstantRepository
    {
        public IList<PaymentTypeModel> GetPaymentTypes()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<PaymentTypeModel>
                    (
                        @"SELECT * 
                        FROM [PaymentTypes]",
                        new DynamicParameters()
                    );

                return results.ToList();
            }
        }

        public IList<UserRoleModel> GetUserRoles()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<UserRoleModel>
                    (
                        @"SELECT * 
                        FROM [UserRoles]",
                        new DynamicParameters()
                    );

                return results.ToList();
            }
        }

        public IList<ProductCategoryModel> GetProductCategories()
        {
            using (var connection = GetDatabaseConnection())
            {
                connection.Open();

                var results = connection.Query<ProductCategoryModel>
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
