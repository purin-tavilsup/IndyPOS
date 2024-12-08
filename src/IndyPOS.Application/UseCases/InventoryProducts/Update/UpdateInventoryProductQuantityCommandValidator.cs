using FluentValidation;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public class UpdateInventoryProductQuantityCommandValidator : AbstractValidator<UpdateInventoryProductQuantityCommand>
{
	public UpdateInventoryProductQuantityCommandValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Inventory Product ID is invalid.");
	}
}