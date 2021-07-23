using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Enums;
using IndyPOS.Events;
using IndyPOS.Extensions;
using IndyPOS.Inventory;
using IndyPOS.SaleInvoice;
using Prism.Events;
using System.Collections.Generic;
using System.Linq;

namespace IndyPOS.Controllers
{
	public class SaleInvoiceController : ISaleInvoiceController
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IInvoiceRepository _invoicesRepository;
        private readonly IInventoryProductRepository _inventoryProductsRepository;

        public IList<ISaleInvoiceProduct> Products { get; }

        public IList<IPayment> Payments { get; }

        public decimal InvoiceTotal => Products.Sum(p => p.Quantity * p.UnitPrice);

        public decimal PaymentTotal => Payments.Sum(p => p.Amount);

        public decimal Changes => CalculateChanges();

        public SaleInvoiceController(IEventAggregator eventAggregator, 
            IInvoiceRepository invoicesRepository, 
            IInventoryProductRepository inventoryProductsRepository)
        {
            _eventAggregator = eventAggregator;
            _invoicesRepository = invoicesRepository;
            _inventoryProductsRepository = inventoryProductsRepository;
            Products = new List<ISaleInvoiceProduct>();
            Payments = new List<IPayment>();
        }

        public void StartNewSale()
        {
            ClearProductsAndPayments();

            _eventAggregator.GetEvent<NewSaleStartedEvent>().Publish();
        }

        public void ClearProductsAndPayments()
		{
            Products.Clear();
            Payments.Clear();
        }

        private decimal CalculateChanges()
		{
            var amount = PaymentTotal - InvoiceTotal;

            return amount >= 0 ? amount : 0m;
        }

        public bool AddProduct(string barcode)
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

        public bool RemoveProduct(string barcode)
        {
            var success = true;
            var productToRemove = Products.FirstOrDefault(p => p.Barcode == barcode);

            if (productToRemove == null)
                return !success;

            Products.Remove(productToRemove);

            _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Publish(barcode);

            return success;
        }

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
        {
            var result = _inventoryProductsRepository.GetProductByBarcode(barcode);

            return result != null ? new InventoryProductAdapter(result) : null;
        }

        public bool AddPayment(PaymentType paymentType, decimal paymentAmount)
		{
            var success = true;

            var payment = new Payment
			{
				PaymentTypeId = (int)paymentType,
				Amount = paymentAmount
			};

            Payments.Add(payment);

            _eventAggregator.GetEvent<PaymentAddedEvent>().Publish();

            return success;
        }

        public bool UpdateProductQuantity(string barcode, int quantity)
		{
            var success = true;
            var productToUpdate = Products.FirstOrDefault(p => p.Barcode == barcode);

            if (productToUpdate == null)
                return !success;

            productToUpdate.Quantity = quantity;

            _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish(barcode);

            return success;
        }
    }
}
