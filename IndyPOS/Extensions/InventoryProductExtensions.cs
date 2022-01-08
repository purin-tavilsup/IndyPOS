using IndyPOS.Inventory;
using IndyPOS.Sales;

namespace IndyPOS.Extensions
{
	public static class InventoryProductExtensions
    {
        public static ISaleInvoiceProduct ToSaleInvoiceProduct(this IInventoryProduct inventoryProduct)
        {
            var saleInvoiceProduct = new SaleInvoiceProduct
            {
                InventoryProductId = inventoryProduct.InventoryProductId,
                Barcode = inventoryProduct.Barcode,
                Description = inventoryProduct.Description,
                Manufacturer = inventoryProduct.Manufacturer,
                Brand = inventoryProduct.Brand,
                Category = inventoryProduct.Category,
                UnitPrice = inventoryProduct.UnitPrice,
                Quantity = 1,
                GroupPrice = inventoryProduct.GroupPrice,
                GroupPriceQuantity = inventoryProduct.GroupPriceQuantity,
				IsTrackable = inventoryProduct.IsTrackable
            };

            return saleInvoiceProduct;
        }
    }
}
