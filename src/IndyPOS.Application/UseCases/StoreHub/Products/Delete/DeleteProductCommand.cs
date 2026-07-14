using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Delete;

/// <summary>
/// Command to soft-delete a product in StoreHub.
/// Sets IsActive = false (preserves history).
/// </summary>
public record DeleteProductCommand(Guid Id) : ICommand;
