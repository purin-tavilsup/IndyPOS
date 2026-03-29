using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Delete;

/// <summary>
/// Legacy command for SQLite inventory products. Uses int ID.
/// For StoreHub, use DeleteProductCommand instead.
/// </summary>
[Obsolete("Use DeleteProductCommand for StoreHub mode")]
public record DeleteInventoryProductCommand(int Id) : ICommand;