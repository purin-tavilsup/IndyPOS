using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.Adapters;

namespace IndyPOS.Extensions
{
    public static class IInventoryProductExtensions
    {
        public static ISaleInvoiceProduct ToSaleInvoiceProduct(this IInventoryProduct inventoryProduct)
        {
            var saleInvoiceProduct = new SaleInvoiceProduct
            {
                InventoryProductId = inventoryProduct.InventoryProductId,
                SKU = inventoryProduct.SKU,
                Barcode = inventoryProduct.Barcode,
                Description = inventoryProduct.Description,
                Manufacturer = inventoryProduct.Manufacturer,
                Brand = inventoryProduct.Brand,
                Category = inventoryProduct.Category,
                UnitCost = inventoryProduct.UnitCost,
                UnitPrice = inventoryProduct.UnitPrice,
                Quantity = 1
            };

            return saleInvoiceProduct;
        }
    }
}
