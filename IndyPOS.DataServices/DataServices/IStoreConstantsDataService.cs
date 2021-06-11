using IndyPOS.DataServices.Models;
using System.Collections.Generic;

namespace IndyPOS.DataServices
{
    public interface IStoreConstantsDataService
    {
        IList<UserRoleModel> GetUserRoles();

        IList<PaymentTypeModel> GetPaymentTypes();

        IList<ProductCategoryModel> GetProductCategories();
    }
}
