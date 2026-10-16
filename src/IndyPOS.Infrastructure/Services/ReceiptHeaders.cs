using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Common.Receipts;

namespace IndyPOS.Infrastructure.Services;

/// <summary>The store lines every receipt starts with, from the till's store configuration.</summary>
public static class ReceiptHeaders
{
    public static ReceiptHeader From(StoreConfiguration config) => new(
        config.StoreName ?? string.Empty,
        config.StoreAddressLine1 ?? string.Empty,
        config.StoreAddressLine2 ?? string.Empty,
        config.StorePhoneNumber ?? string.Empty);
}
