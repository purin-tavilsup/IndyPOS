using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
    public interface IInventoryProductRepository
    {
        InventoryProductModel GetProductByBarcode(string barcode);

        IList<InventoryProductModel> GetProductsByCategoryId(int categoryId);

        void AddProduct(InventoryProductModel product);

        void UpdateProduct(InventoryProductModel product);

        void RemoveProduct(InventoryProductModel product);

        void RemoveProductByBarcode(string barcode);
    }
}
