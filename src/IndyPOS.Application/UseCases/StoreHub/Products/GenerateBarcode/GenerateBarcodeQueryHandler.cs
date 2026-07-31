using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.ValueObjects;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.GenerateBarcode;

/// <summary>
/// Handler for generating the next barcode.
/// Format: 200 + {StoreCode:D2} + {Sequence:D7} + EAN-13 check digit (e.g., "2000100000038").
/// <para>
/// This previously returned <c>{StoreCode}{Sequence:D8}</c>, which is 9 digits for a
/// single-digit store code. EAN-13 accepts only 12 or 13, so every generated barcode
/// crashed the label preview in the add-product dialog. See <see cref="Ean13Barcode"/>.
/// </para>
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

        var storeCode = _storeIdentityService.StoreCode;

        return Ean13Barcode.ForStoreProduct(storeCode, nextValue);
    }
}
