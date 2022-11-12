using IndyPOS.Facade.Interfaces;
using IndyPOS.Interfaces;

namespace IndyPOS.Controllers
{
    public class InventoryController : IInventoryController
    {
        private readonly IInventoryHelper _inventoryHelper;

        public InventoryController(IInventoryHelper inventoryHelper)
        {
            _inventoryHelper = inventoryHelper;
        }

        public IList<IInventoryProduct> GetInventoryProductsByCategoryId(int id)
        {
            return _inventoryHelper.GetInventoryProductsByCategoryId(id);
		}

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
        {
            return _inventoryHelper.GetInventoryProductByBarcode(barcode);
		}

        public IInventoryProduct GetProductById(int id)
		{
			return _inventoryHelper.GetProductById(id);
		}

        public void AddNewProduct(IInventoryProduct product)
        {
			_inventoryHelper.AddNewProduct(product);
		}

        public void UpdateProduct(IInventoryProduct product)
        {
			_inventoryHelper.UpdateProduct(product);
		}

        public void RemoveProductById(int id)
		{
			_inventoryHelper.RemoveProductById(id);
		}

        public int GetProductBarcodeCounter()
		{
			return _inventoryHelper.GetProductBarcodeCounter();
		}

        public void IncrementProductBarcodeCounter()
		{
			_inventoryHelper.IncrementProductBarcodeCounter();
        }
    }
}
