using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.DataServices
{
    public interface IInventoryProductsDataService
    {
        InventoryProductModel GetProductByBarcode(string barcode);

        IList<InventoryProductModel> GetProductsByCategoryId(int categoryId);
    }
}
