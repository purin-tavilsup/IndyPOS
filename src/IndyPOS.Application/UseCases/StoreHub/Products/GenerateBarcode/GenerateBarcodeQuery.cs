using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.GenerateBarcode;

/// <summary>
/// Query to generate the next barcode for the store.
/// Atomically increments the store's barcode counter.
/// </summary>
public record GenerateBarcodeQuery : IQuery<string>;
