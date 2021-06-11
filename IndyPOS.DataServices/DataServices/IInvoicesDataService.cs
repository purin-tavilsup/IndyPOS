using IndyPOS.DataServices.Models;
using System.Collections.Generic;

namespace IndyPOS.DataServices
{
    public interface IInvoicesDataService
    {
        IList<InventoryProductModel> GetProductsForSale();
    }
}
