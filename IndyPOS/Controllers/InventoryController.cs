﻿using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Events;
using IndyPOS.Inventory;
using Prism.Events;
using System;
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

        public IInventoryProduct GetProductByInventoryProductId(int id)
        {
            var result = _inventoryProductsRepository.GetProductByInventoryProductId(id);

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
                UnitCost = product.UnitCost,
                UnitPrice = product.UnitPrice,
                QuantityInStock = product.QuantityInStock
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
                UnitCost = product.UnitCost,
                UnitPrice = product.UnitPrice,
                QuantityInStock = product.QuantityInStock
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

        public void RemoveProductByInventoryProductId(int id)
		{
            try
            {
                _inventoryProductsRepository.RemoveProductByInventoryProductId(id);

                _eventAggregator.GetEvent<InventoryProductDeletedEvent>().Publish();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while trying to delete the inventory product. {ex.Message}", ex);
            }
        }
    }
}
