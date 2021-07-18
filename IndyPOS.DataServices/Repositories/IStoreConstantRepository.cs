using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IStoreConstantRepository
    {
        IList<UserRoleModel> GetUserRoles();

        IList<PaymentTypeModel> GetPaymentTypes();

        IList<ProductCategoryModel> GetProductCategories();
    }
}
