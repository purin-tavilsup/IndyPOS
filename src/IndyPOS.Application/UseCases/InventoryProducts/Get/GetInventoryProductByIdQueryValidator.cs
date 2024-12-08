using FluentValidation;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductByIdQueryValidator : AbstractValidator<GetInventoryProductByIdQuery>
{
	public GetInventoryProductByIdQueryValidator()
	{
		RuleFor(x => x.Id)
			.GreaterThan(0).WithMessage("Inventory Product Id is invalid.");
	}
}