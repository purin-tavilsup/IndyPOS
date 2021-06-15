using IndyPOS.DataServices.Models;
using System.Collections.Generic;

namespace IndyPOS.DataServices
{
    public interface IInventoryProductsDataService
    {
        InventoryProductModel GetProductByBarcode(string barcode);

        IList<InventoryProductModel> GetProductsByCategoryId(int categoryId);

        void AddProduct(InventoryProductModel product);

        void UpdateProduct(InventoryProductModel product);

        void RemoveProduct(InventoryProductModel product);

        void RemoveProductByBarcode(string barcode);
    }
}
