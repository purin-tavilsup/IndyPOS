using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;

namespace IndyPOS.Application.UseCases.StoreHub.PaymentMethods;

/// <summary>
/// Composes the payment-method repository with the store-type offerability
/// policy, and hosts the admin mutations (add campaign, toggle, edit display).
/// </summary>
public class PaymentMethodCatalogService : IPaymentMethodCatalogService
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IStoreIdentityService _storeIdentity;

    public PaymentMethodCatalogService(IPaymentMethodRepository repository, IStoreIdentityService storeIdentity)
    {
        _repository = repository;
        _storeIdentity = storeIdentity;
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetOfferableAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        return PaymentMethodPolicy.Offerable(all, _storeIdentity.StoreType);
    }

    public Task<IReadOnlyList<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task AddCampaignAsync(string code, string displayName, int displayOrder, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByCodeAsync(code, cancellationToken) is not null)
            throw new InvalidOperationException($"Payment method '{code}' already exists.");

        var now = DateTime.UtcNow;
        await _repository.AddAsync(new PaymentMethod
        {
            Code = code, DisplayName = displayName, Kind = PaymentMethodKind.GovernmentCampaign,
            IsEnabled = true, DisplayOrder = displayOrder, StoreId = _storeIdentity.StoreId,
            CreatedUtc = now, LastModifiedUtc = now
        }, cancellationToken);
    }

    public async Task SetEnabledAsync(string code, bool enabled, CancellationToken cancellationToken = default)
    {
        var method = await Require(code, cancellationToken);
        method.IsEnabled = enabled;
        method.LastModifiedUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(method, cancellationToken);
    }

    public async Task UpdateDisplayAsync(string code, string displayName, int? displayOrder, CancellationToken cancellationToken = default)
    {
        var method = await Require(code, cancellationToken);
        method.DisplayName = displayName;
        if (displayOrder is int order)
            method.DisplayOrder = order;
        method.LastModifiedUtc = DateTime.UtcNow;
        await _repository.UpdateAsync(method, cancellationToken);
    }

    private async Task<PaymentMethod> Require(string code, CancellationToken cancellationToken) =>
        await _repository.GetByCodeAsync(code, cancellationToken)
        ?? throw new InvalidOperationException($"Payment method '{code}' not found.");
}
