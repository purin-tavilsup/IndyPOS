using FluentValidation;

namespace IndyPOS.Application.InventoryProducts.Commands.Delete;

public class DeleteInventoryProductCommandValidator : AbstractValidator<DeleteInventoryProductCommand>
{
	public DeleteInventoryProductCommandValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Inventory Product ID is invalid.");
	}
}