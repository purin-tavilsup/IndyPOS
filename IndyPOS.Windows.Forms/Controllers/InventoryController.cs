using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.InventoryProducts.Queries;
using IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductsByCategoryId;
using IndyPOS.Windows.Forms.Interfaces;
using MediatR;

namespace IndyPOS.Windows.Forms.Controllers
{
    public class InventoryController : IInventoryController
    {
        private readonly IInventoryHelper _inventoryHelper;
		private readonly IMediator _mediator;

        public InventoryController(IInventoryHelper inventoryHelper, IMediator mediator)
        {
            _inventoryHelper = inventoryHelper;
            _mediator = mediator;
        }

        public IList<IInventoryProduct> GetInventoryProductsByCategoryId(int id)
        {
            return _inventoryHelper.GetInventoryProductsByCategoryId(id);
		}

        public async Task<IEnumerable<InventoryProductDto>> GetInventoryProductsByCategoryIdAsync(int id)
		{
			var results = await _mediator.Send(new GetInventoryProductsByCategoryIdQuery(id));

            return results;
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
