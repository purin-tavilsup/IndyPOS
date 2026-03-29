using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.GenerateBarcode;

/// <summary>
/// Handler for generating the next barcode.
/// Format: {StoreCode}{Sequence:D8} (e.g., "100000001")
/// </summary>
public class GenerateBarcodeQueryHandler : IQueryHandler<GenerateBarcodeQuery, string>
{
    private readonly IStoreSettingRepository _settingRepository;
    private readonly IStoreIdentityService _storeIdentityService;

    public GenerateBarcodeQueryHandler(
        IStoreSettingRepository settingRepository,
        IStoreIdentityService storeIdentityService)
    {
        _settingRepository = settingRepository;
        _storeIdentityService = storeIdentityService;
    }

    public async Task<string> HandleAsync(
        GenerateBarcodeQuery query,
        CancellationToken cancellationToken = default)
    {
        // Atomically increment the counter
        var nextValue = await _settingRepository.IncrementAsync(
            StoreSettingKeys.BarcodeCounter, cancellationToken);

        // Format: {StoreCode}{Sequence:D8}
        var storeCode = _storeIdentityService.StoreCode;
        var barcode = $"{storeCode}{nextValue:D8}";

        return barcode;
    }
}
