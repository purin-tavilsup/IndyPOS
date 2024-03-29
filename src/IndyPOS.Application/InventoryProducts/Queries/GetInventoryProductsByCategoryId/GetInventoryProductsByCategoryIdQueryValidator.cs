﻿using FluentValidation;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByCategoryId;

public class GetInventoryProductsByCategoryIdQueryValidator : AbstractValidator<GetInventoryProductsByCategoryIdQuery>
{
	public GetInventoryProductsByCategoryIdQueryValidator()
	{
		RuleFor(x => x.CategoryId)
			.GreaterThan(0).WithMessage("Category Id is invalid.");
	}
}