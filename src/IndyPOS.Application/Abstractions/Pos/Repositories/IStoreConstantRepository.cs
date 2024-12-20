﻿using IndyPOS.Domain.Entities;

namespace IndyPOS.Application.Abstractions.Pos.Repositories;

public interface IStoreConstantRepository
{
	IEnumerable<UserRole> GetUserRoles();

	IEnumerable<PaymentType> GetPaymentTypes();

	IEnumerable<ProductCategory> GetProductCategories();
}