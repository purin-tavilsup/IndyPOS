using IndyPOS.DataAccess.Models;
using System.Collections.Generic;

namespace IndyPOS.DataAccess.Repositories
{
	public interface IInvoiceRepository
    {
        IList<InventoryProduct> GetProductsForSale();
    }
}
