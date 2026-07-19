using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>Seeds the known payment methods for this store. Idempotent — only
/// inserts a Code that is absent, so re-runs (and future new campaigns) are safe.</summary>
public class PaymentMethodSeeder
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<PaymentMethodSeeder> _logger;

    public PaymentMethodSeeder(IPaymentMethodRepository repository, IStoreIdentityService storeIdentity,
        ILogger<PaymentMethodSeeder> logger)
    {
        _repository = repository;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    private sealed record Seed(string Code, string DisplayName, PaymentMethodKind Kind, bool Enabled, int Order);

    private static readonly Seed[] Defaults =
    [
        new(PaymentMethodCodes.Cash, "เงินสด", PaymentMethodKind.Permanent, true, 1),
        new(PaymentMethodCodes.MoneyTransfer, "เงินโอน", PaymentMethodKind.Permanent, true, 2),
        new(PaymentMethodCodes.WelfareCard, "บัตรสวัสดิการแห่งรัฐ", PaymentMethodKind.Permanent, true, 3),
        new(PaymentMethodCodes.PayLater, "เงินเชื่อ", PaymentMethodKind.Permanent, true, 4),
        new(PaymentMethodCodes.M33WeLove, "ม33เรารักกัน", PaymentMethodKind.GovernmentCampaign, false, 5),
        new(PaymentMethodCodes.FiftyFifty, "คนละครึ่ง", PaymentMethodKind.GovernmentCampaign, false, 6),
        new(PaymentMethodCodes.WeWin, "เราชนะ", PaymentMethodKind.GovernmentCampaign, false, 7),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var storeId = _storeIdentity.StoreId;
        var now = DateTime.UtcNow;
        var seededCount = 0;

        foreach (var seed in Defaults)
        {
            if (await _repository.GetByCodeAsync(seed.Code, cancellationToken) is not null) continue;

            await _repository.AddAsync(new PaymentMethod
            {
                Code = seed.Code,
                DisplayName = seed.DisplayName,
                Kind = seed.Kind,
                IsEnabled = seed.Enabled,
                DisplayOrder = seed.Order,
                StoreId = storeId,
                CreatedUtc = now,
                LastModifiedUtc = now
            }, cancellationToken);
            seededCount++;
        }

        _logger.LogInformation("Payment method seeding complete: {Count} inserted.", seededCount);
    }
}
