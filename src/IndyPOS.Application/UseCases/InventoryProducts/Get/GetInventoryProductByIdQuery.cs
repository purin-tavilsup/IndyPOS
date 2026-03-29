using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

/// <summary>
/// Legacy query for SQLite inventory products. Uses int ID.
/// For StoreHub, use IProductCacheService.GetById(Guid) instead.
/// </summary>
[Obsolete("Use IProductCacheService.GetById(Guid) for StoreHub mode")]
public record GetInventoryProductByIdQuery(int Id) : IQuery<InventoryProductDto>;