using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IInventoryProductRepository
    {
        InventoryProduct GetProductByBarcode(string barcode);

        IList<InventoryProduct> GetProductsByCategoryId(int categoryId);

        void AddProduct(InventoryProduct product);

        void UpdateProduct(InventoryProduct product);

        void RemoveProduct(InventoryProduct product);

        void RemoveProductByBarcode(string barcode);
    }
}
