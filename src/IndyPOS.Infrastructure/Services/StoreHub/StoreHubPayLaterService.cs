using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.PayLater;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// StoreHub-based implementation of IPayLaterService.
/// Delegates to the StoreHub API via IStoreHubClient.
/// </summary>
public class StoreHubPayLaterService : IPayLaterService
{
    private readonly IStoreHubClient _storeHubClient;

    public StoreHubPayLaterService(IStoreHubClient storeHubClient)
    {
        _storeHubClient = storeHubClient;
    }

    public async Task<GetPayLaterResponse> GetAllAsync(
        bool includeCompleted = false,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        return await _storeHubClient.GetPayLaterAsync(includeCompleted, searchTerm, cancellationToken);
    }

    public async Task<PayLaterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _storeHubClient.GetPayLaterByIdAsync(id, cancellationToken);
    }

    public async Task<PayLaterDto> RecordPaymentAsync(
        Guid payLaterId,
        decimal paymentAmount,
        CancellationToken cancellationToken = default)
    {
        return await _storeHubClient.RecordPayLaterPaymentAsync(payLaterId, paymentAmount, cancellationToken);
    }
}
