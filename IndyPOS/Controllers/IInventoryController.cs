using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.Adapters;

namespace IndyPOS.Controllers
{
    public interface IInventoryController
    {
        IList<IInventoryProduct> GetInventoryProductsByCategoryId(int categoryId);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);
    }
}
