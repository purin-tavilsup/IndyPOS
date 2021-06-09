using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndyPOS.DataServices;
using IndyPOS.Adapters;
using IndyPOS.Extensions;
using Prism.Events;
using IndyPOS.EventAggregator;

namespace IndyPOS.Controllers
{
    public class InventoryController
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInventoryProductsDataService _inventoryProductsDataService;

        public InventoryController(IEventAggregator eventAggregator, IInventoryProductsDataService inventoryProductsDataService)
        {
            _eventAggregator = eventAggregator;
            _inventoryProductsDataService = inventoryProductsDataService;
        }

        public IList<IInventoryProduct> GetInventoryProductsByCategoryId(int categoryId)
        {
            var results = _inventoryProductsDataService.GetProductsByCategoryId(categoryId);

            return results.Select(p => new InventoryProductAdapter(p) as IInventoryProduct).ToList();
        }

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
        {
            var result = _inventoryProductsDataService.GetProductByBarcode(barcode);

            return result != null ? new InventoryProductAdapter(result) : null;
        }
    }
}
