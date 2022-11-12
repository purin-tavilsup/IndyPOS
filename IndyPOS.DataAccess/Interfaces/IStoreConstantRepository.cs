using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces
{
    public interface IStoreConstantRepository
    {
        IList<UserRole> GetUserRoles();

        IList<PaymentType> GetPaymentTypes();

        IList<ProductCategory> GetProductCategories();
    }
}
