using IndyPOS.DataServices.Models;
using System.Collections.Generic;

namespace IndyPOS.DataServices
{
    public interface IInventoryProductsDataService
    {
        InventoryProductModel GetProductByBarcode(string barcode);

        IList<InventoryProductModel> GetProductsByCategoryId(int categoryId);
    }
}
