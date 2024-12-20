﻿using FluentValidation;

namespace IndyPOS.Application.UseCases.InventoryProducts.Create;

public class CreateInventoryProductCommandValidator : AbstractValidator<CreateInventoryProductCommand>
{
	public CreateInventoryProductCommandValidator()
	{
		RuleFor(x => x.Category)
			.GreaterThan(0).WithMessage("Product category is invalid.");

		RuleFor(x => x.Barcode)
			.NotEmpty().WithMessage("Product barcode cannot be empty.");

		RuleFor(x => x.Description)
			.NotEmpty().WithMessage("Product description cannot be empty.");

		RuleFor(x => x.QuantityInStock)
			.GreaterThan(0).WithMessage("Product quantity is invalid.");
	}
}