using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.Adapters;
using IndyPOS.Inventory;

namespace IndyPOS.Controllers
{
    public interface IInventoryController
    {
        IList<IInventoryProduct> GetInventoryProductsByCategoryId(int categoryId);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        IInventoryProduct GetProductByInventoryProductId(int id);

        void AddNewProduct(IInventoryProduct product);

        void UpdateProduct(IInventoryProduct product);

        void RemoveProductByBarcode(string barcode);
    }
}
