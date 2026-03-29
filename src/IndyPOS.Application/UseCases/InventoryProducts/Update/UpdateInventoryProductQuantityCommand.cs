using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

/// <summary>
/// Legacy command for SQLite inventory products. Uses int ID.
/// For StoreHub, use AdjustProductQuantityCommand instead.
/// </summary>
[Obsolete("Use AdjustProductQuantityCommand for StoreHub mode")]
public class UpdateInventoryProductQuantityCommand : ICommand
{
	public int Id { get; set; }

	public int Quantity { get; set; }
}