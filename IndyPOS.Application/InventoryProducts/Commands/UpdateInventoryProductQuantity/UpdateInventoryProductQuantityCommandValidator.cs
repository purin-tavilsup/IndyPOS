using FluentValidation;

namespace IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProductQuantity;

public class UpdateInventoryProductQuantityCommandValidator : AbstractValidator<UpdateInventoryProductQuantityCommand>
{
	public UpdateInventoryProductQuantityCommandValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Inventory Product Id is invalid.");
	}
}