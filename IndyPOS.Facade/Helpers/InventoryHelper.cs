using IndyPOS.DataAccess.Interfaces;
using IndyPOS.Facade.Adapters;
using IndyPOS.Facade.Events;
using IndyPOS.Facade.Interfaces;
using Prism.Events;

namespace IndyPOS.Facade.Helpers;

public class InventoryHelper : IInventoryHelper
{
	private readonly IEventAggregator _eventAggregator;
	private readonly IInventoryProductRepository _inventoryProductsRepository;

	public InventoryHelper(IEventAggregator eventAggregator, IInventoryProductRepository inventoryProductsRepository)
	{
		_eventAggregator = eventAggregator;
		_inventoryProductsRepository = inventoryProductsRepository;
	}

	public IList<IInventoryProduct> GetInventoryProductsByCategoryId(int id)
	{
		var results = _inventoryProductsRepository.GetProductsByCategoryId(id);

		return results.Select(p => new InventoryProductAdapter(p) as IInventoryProduct).ToList();
	}

	public IInventoryProduct GetInventoryProductByBarcode(string barcode)
	{
		var result = _inventoryProductsRepository.GetProductByBarcode(barcode);

		return new InventoryProductAdapter(result);
	}

	public IInventoryProduct GetProductById(int id)
	{
		var result = _inventoryProductsRepository.GetProductById(id);

		return new InventoryProductAdapter(result);
	}

	public void AddNewProduct(IInventoryProduct product)
	{
		var productModel = new DataAccess.Models.InventoryProduct
		{
			Barcode = product.Barcode,
			Description = product.Description,
			Manufacturer = product.Manufacturer,
			Brand = product.Brand,
			Category = product.Category,
			UnitPrice = product.UnitPrice,
			QuantityInStock = product.QuantityInStock,
			IsTrackable = product.IsTrackable
		};

		var inventoryProductId = _inventoryProductsRepository.AddProduct(productModel);

		_eventAggregator.GetEvent<InventoryProductAddedEvent>().Publish(inventoryProductId);
	}

	public void UpdateProduct(IInventoryProduct product)
	{
		var productModel = new DataAccess.Models.InventoryProduct
		{
			InventoryProductId = product.InventoryProductId,
			Barcode = product.Barcode,
			Description = product.Description,
			Manufacturer = product.Manufacturer,
			Brand = product.Brand,
			Category = product.Category,
			UnitPrice = product.UnitPrice,
			QuantityInStock = product.QuantityInStock,
			GroupPrice = product.GroupPrice,
			GroupPriceQuantity = product.GroupPriceQuantity
		};

		_inventoryProductsRepository.UpdateProduct(productModel);

		_eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(productModel.InventoryProductId);
	}

	public void RemoveProductById(int id)
	{
		_inventoryProductsRepository.RemoveProductById(id);

		_eventAggregator.GetEvent<InventoryProductDeletedEvent>().Publish();
	}

	public int GetProductBarcodeCounter()
	{
		return _inventoryProductsRepository.GetProductBarcodeCounter();
	}

	public void IncrementProductBarcodeCounter()
	{
		var counter = _inventoryProductsRepository.GetProductBarcodeCounter();

		_inventoryProductsRepository.UpdateProductBarcodeCounter(counter + 1);
	}
}