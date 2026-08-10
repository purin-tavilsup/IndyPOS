using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.GetStock;

public class GetProductStockQueryHandler
    : IQueryHandler<GetProductStockQuery, IReadOnlyList<ProductStockDto>>
{
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IStoreIdentityService _storeIdentityService;

    public GetProductStockQueryHandler(
        IInventoryMovementRepository movementRepository,
        IStoreIdentityService storeIdentityService)
    {
        _movementRepository = movementRepository;
        _storeIdentityService = storeIdentityService;
    }

    public async Task<IReadOnlyList<ProductStockDto>> HandleAsync(
        GetProductStockQuery query,
        CancellationToken cancellationToken = default)
    {
        var balances = await _movementRepository.GetBalancesAsync(
            _storeIdentityService.StoreId, cancellationToken);

        if (query.ProductId is { } productId)
        {
            return balances.TryGetValue(productId, out var quantity)
                ? [new ProductStockDto(productId, quantity)]
                : [];
        }

        return balances.Select(b => new ProductStockDto(b.Key, b.Value))
                       .ToList();
    }
}
