using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public class GetAllPaymentMethodsQueryHandler
    : IQueryHandler<GetAllPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>
{
    private readonly IPaymentMethodCatalogService _catalogService;

    public GetAllPaymentMethodsQueryHandler(IPaymentMethodCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IReadOnlyList<PaymentMethodDto>> HandleAsync(
        GetAllPaymentMethodsQuery query,
        CancellationToken cancellationToken = default)
    {
        var methods = await _catalogService.GetAllAsync(cancellationToken);
        return methods.ToDtos();
    }
}
