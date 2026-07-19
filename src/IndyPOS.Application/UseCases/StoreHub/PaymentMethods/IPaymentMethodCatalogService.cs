using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

public interface IPaymentMethodCatalogService
{
    Task<IReadOnlyList<PaymentMethod>> GetOfferableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddCampaignAsync(string code, string displayName, int displayOrder, CancellationToken cancellationToken = default);
    Task SetEnabledAsync(string code, bool enabled, CancellationToken cancellationToken = default);
    Task UpdateDisplayAsync(string code, string displayName, int? displayOrder, CancellationToken cancellationToken = default);
}
