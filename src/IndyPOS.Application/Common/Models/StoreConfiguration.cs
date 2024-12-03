#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class StoreConfiguration
{
    public string? StoreFullName { get; init; }
    public string? StoreName { get; init; }
    public string? StoreAddressLine1 { get; init; }
    public string? StoreAddressLine2 { get; init; }
    public string? StorePhoneNumber { get; init; }
    public string? PrinterName { get; init; }
    public string? BarcodeScannerDeviceName { get; init; }
    public bool? CloudDatabaseEnabled { get; init; }

    public string? SerialPortName { get; init; } = "COM1";

    public int? Code { get; init; } = 1;
}