using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public class GetOfferablePaymentMethodsQueryHandler
    : IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>
{
    private readonly IPaymentMethodCatalogService _catalogService;

    public GetOfferablePaymentMethodsQueryHandler(IPaymentMethodCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IReadOnlyList<PaymentMethodDto>> HandleAsync(
        GetOfferablePaymentMethodsQuery query,
        CancellationToken cancellationToken = default)
    {
        var methods = await _catalogService.GetOfferableAsync(cancellationToken);
        return methods.ToDtos();
    }
}
