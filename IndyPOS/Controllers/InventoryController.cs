using IndyPOS.Adapters;
using IndyPOS.DataServices;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Controllers
{
    public class InventoryController : IInventoryController
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

        public void AddNewProduct(IInventoryProduct product)
        {
            //
        }

        public void UpdateProduct(IInventoryProduct product)
        {
            //
        }

        public void RemoveProductByBarcode(string barcode)
		{
            //
		}
    }
}
