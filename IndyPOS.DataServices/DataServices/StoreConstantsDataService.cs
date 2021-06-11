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
    public class StoreConstantsDataService : SQLiteDatabase, IStoreConstantsDataService
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
