using IndyPOS.Inventory;
using System.Collections.Generic;

namespace IndyPOS.Controllers
{
	public interface IInventoryController
    {
        IList<IInventoryProduct> GetInventoryProductsByCategoryId(int id);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        IInventoryProduct GetProductByInventoryProductId(int id);

        void AddNewProduct(IInventoryProduct product);

        void UpdateProduct(IInventoryProduct product);

        void RemoveProductByInventoryProductId(int id);
    }
}
