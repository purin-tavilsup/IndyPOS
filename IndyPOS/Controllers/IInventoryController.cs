using IndyPOS.Inventory;
using System.Collections.Generic;

namespace IndyPOS.Controllers
{
	public interface IInventoryController
    {
        IList<IInventoryProduct> GetInventoryProductsByCategoryId(int id);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        IInventoryProduct GetProductById(int id);

        void AddNewProduct(IInventoryProduct product);

        void UpdateProduct(IInventoryProduct product);

        void RemoveProductById(int id);
    }
}
