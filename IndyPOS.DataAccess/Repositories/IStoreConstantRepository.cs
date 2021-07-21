using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IStoreConstantRepository
    {
        IList<UserRole> GetUserRoles();

        IList<PaymentType> GetPaymentTypes();

        IList<ProductCategory> GetProductCategories();
    }
}
