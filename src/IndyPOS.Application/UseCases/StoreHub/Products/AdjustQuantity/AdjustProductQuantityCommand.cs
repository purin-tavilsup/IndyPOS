using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Command to adjust product quantity via inventory movement.
/// Calculates delta between current and target quantity.
/// </summary>
public record AdjustProductQuantityCommand : ICommand<int>
{
    public required Guid ProductId { get; init; }
    public required int TargetQuantity { get; init; }
    public string? Reason { get; init; }
}
