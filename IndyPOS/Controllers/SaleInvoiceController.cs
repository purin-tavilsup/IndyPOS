using IndyPOS.Adapters;
using IndyPOS.DataServices;
using IndyPOS.Events;
using IndyPOS.Extensions;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Controllers
{
    public class SaleInvoiceController : ISaleInvoiceController
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInvoicesDataService _invoicesDataService;
        private readonly IInventoryProductsDataService _inventoryProductsDataService;

        public IList<ISaleInvoiceProduct> Products { get; }

        public decimal InvoiceTotal => Products.Sum(p => p.Quantity * p.UnitPrice);

        public SaleInvoiceController(IEventAggregator eventAggregator, 
            IInvoicesDataService invoicesDataService, 
            IInventoryProductsDataService inventoryProductsDataService)
        {
            _eventAggregator = eventAggregator;
            _invoicesDataService = invoicesDataService;
            _inventoryProductsDataService = inventoryProductsDataService;
            Products = new List<ISaleInvoiceProduct>();
        }

        public void StartNewSaleInvoice()
        {
            Products.Clear();
        }

        public bool AddProductToSaleInvoice(string barcode)
        {
            var success = true;
            var product = GetInventoryProductByBarcode(barcode);

            if (product == null)
                return !success;

            var existingProduct = Products.FirstOrDefault(p => p.Barcode == barcode);

            if (existingProduct == null)
            {
                Products.Add(product.ToSaleInvoiceProduct());
            }
            else
            {
                existingProduct.Quantity++;
            }
            
            _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish(barcode);

            return success;
        }

        public bool RemoveProductFromSaleInvoice(string barcode)
        {
            var success = true;
            var productToRemove = Products.FirstOrDefault(p => p.Barcode == barcode);

            if (productToRemove == null)
                return !success;

            if (productToRemove.Quantity == 1)
            {
                Products.Remove(productToRemove);
            }
            else
            {
                productToRemove.Quantity--;
            }

            _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Publish(barcode);

            return success;
        }

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
        {
            var result = _inventoryProductsDataService.GetProductByBarcode(barcode);

            return result != null ? new InventoryProductAdapter(result) : null;
        }
    }
}
