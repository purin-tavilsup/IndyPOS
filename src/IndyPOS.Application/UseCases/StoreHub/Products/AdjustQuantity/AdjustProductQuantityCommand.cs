using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;

/// <summary>
/// Command to adjust product quantity via inventory movement.
/// Writes Delta straight through; it is never derived from a balance read.
/// </summary>
public record AdjustProductQuantityCommand : ICommand<int>
{
    public required Guid ProductId { get; init; }
    public required int Delta { get; init; }
    public string? Reason { get; init; }
}
