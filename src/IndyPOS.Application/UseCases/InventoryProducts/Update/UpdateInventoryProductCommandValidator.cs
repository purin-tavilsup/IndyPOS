﻿using FluentValidation;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public class UpdateInventoryProductCommandValidator : AbstractValidator<UpdateInventoryProductCommand>
{
	public UpdateInventoryProductCommandValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Inventory Product ID is invalid.");

		RuleFor(x => x.Description)
			.NotEmpty().WithMessage("Product description cannot be empty.");

		RuleFor(x => x.Category)
			.GreaterThan(0).WithMessage("Product category is invalid.");

		RuleFor(x => x.UnitPrice)
			.GreaterThan(-1m).WithMessage("Product unit price is invalid.");
	}
}