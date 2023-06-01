using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Common.Interfaces;

public interface IStoreConstantRepository
{
	IEnumerable<UserRole> GetUserRoles();

	IEnumerable<PaymentType> GetPaymentTypes();

	IEnumerable<ProductCategory> GetProductCategories();
}