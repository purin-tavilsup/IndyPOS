using IndyPOS.Adapters;
using IndyPOS.Interfaces;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.Facade.Events;

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

        public IList<IInventoryProduct> GetInventoryProductsByCategoryId(int id)
        {
            var results = _inventoryProductsRepository.GetProductsByCategoryId(id);

            return results.Select(p => new InventoryProductAdapter(p) as IInventoryProduct).ToList();
        }

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
        {
            var result = _inventoryProductsRepository.GetProductByBarcode(barcode);

            return result != null ? new InventoryProductAdapter(result) : null;
        }

        public IInventoryProduct GetProductById(int id)
        {
            var result = _inventoryProductsRepository.GetProductById(id);

            return result != null ? new InventoryProductAdapter(result) : null;
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

            try
			{
                var inventoryProductId = _inventoryProductsRepository.AddProduct(productModel);

                _eventAggregator.GetEvent<InventoryProductAddedEvent>().Publish(inventoryProductId);
            }
            catch(Exception ex)
			{
                throw new Exception($"Error occurred while trying to add the inventory product. {ex.Message}", ex);
			}
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

            try
            {
                _inventoryProductsRepository.UpdateProduct(productModel);

                _eventAggregator.GetEvent<InventoryProductUpdatedEvent>().Publish(productModel.InventoryProductId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while trying to update the inventory product. {ex.Message}", ex);
            }
        }

        public void RemoveProductById(int id)
		{
            try
            {
                _inventoryProductsRepository.RemoveProductById(id);

                _eventAggregator.GetEvent<InventoryProductDeletedEvent>().Publish();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while trying to delete the inventory product. {ex.Message}", ex);
            }
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
}
