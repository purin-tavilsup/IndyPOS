using IndyPOS.DataAccess.Models;

namespace IndyPOS.DataAccess.Interfaces;

public interface IStoreConstantRepository
{
	IEnumerable<UserRole> GetUserRoles();

	IEnumerable<PaymentType> GetPaymentTypes();

	IEnumerable<ProductCategory> GetProductCategories();
}