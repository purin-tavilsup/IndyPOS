using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductByBarcodeQuery(string Barcode) : IQuery<InventoryProductDto>;