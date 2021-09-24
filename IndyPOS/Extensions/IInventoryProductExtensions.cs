﻿using IndyPOS.Inventory;
using IndyPOS.SaleInvoice;

namespace IndyPOS.Extensions
{
	public static class IInventoryProductExtensions
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
                UnitCost = inventoryProduct.UnitCost,
                UnitPrice = inventoryProduct.UnitPrice,
                Quantity = 1,
                GroupPrice = inventoryProduct.GroupPrice,
                GroupPriceQuantity = inventoryProduct.GroupPriceQuantity
            };

            return saleInvoiceProduct;
        }
    }
}
