using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Inventory;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Controllers
{
	public class InventoryController : IInventoryController
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInventoryProductRepository _inventoryProductsRepository;

        public InventoryController(IEventAggregator eventAggregator, IInventoryProductRepository inventoryProductsRepository)
        {
            _eventAggregator = eventAggregator;
            _inventoryProductsRepository = inventoryProductsRepository;
        }

        public IList<IInventoryProduct> GetInventoryProductsByCategoryId(int categoryId)
        {
            var results = _inventoryProductsRepository.GetProductsByCategoryId(categoryId);

            return results.Select(p => new InventoryProductAdapter(p) as IInventoryProduct).ToList();
        }

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
        {
            var result = _inventoryProductsRepository.GetProductByBarcode(barcode);

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
