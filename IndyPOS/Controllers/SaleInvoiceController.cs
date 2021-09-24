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
        private ISaleInvoice _saleInvoice;

        public IReadOnlyCollection<ISaleInvoiceProduct> Products => (IReadOnlyCollection<ISaleInvoiceProduct>)_saleInvoice.Products;

        public IReadOnlyCollection<IPayment> Payments => (IReadOnlyCollection<IPayment>)_saleInvoice.Payments;

        public decimal InvoiceTotal => _saleInvoice.InvoiceTotal;

        public decimal PaymentTotal => _saleInvoice.PaymentTotal;

        public decimal Changes => _saleInvoice.Changes;

        public int UserId { get; set; }

        public int? CustomerId { get; set; }

        public SaleInvoiceController(IEventAggregator eventAggregator,
            IInvoiceRepository invoicesRepository, 
            IInventoryProductRepository inventoryProductsRepository)
        {
            _eventAggregator = eventAggregator;
            _invoicesRepository = invoicesRepository;
            _inventoryProductsRepository = inventoryProductsRepository;
        }

        public void StartNewSale()
        {
            _saleInvoice = new SaleInvoice.SaleInvoice();

            _eventAggregator.GetEvent<NewSaleStartedEvent>().Publish();
        }

        public bool AddProduct(string barcode)
        {
            var success = true;
            var product = GetInventoryProductByBarcode(barcode);

            if (product == null)
                return !success;

            _saleInvoice.Products.Add(product.ToSaleInvoiceProduct());

            _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish(barcode);

            return success;
        }

        public bool RemoveProduct(string barcode)
        {
            var success = true;
            var productToRemove = _saleInvoice.Products.FirstOrDefault(p => p.Barcode == barcode);

            if (productToRemove == null)
                return !success;

            _saleInvoice.Products.Remove(productToRemove);

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

            _saleInvoice.Payments.Add(payment);

            _eventAggregator.GetEvent<PaymentAddedEvent>().Publish();

            return success;
        }

        public bool UpdateProductQuantity(string barcode, int quantity)
		{
            var success = true;
            var productToUpdate = _saleInvoice.Products.FirstOrDefault(p => p.Barcode == barcode);

            if (productToUpdate == null)
                return !success;

            productToUpdate.Quantity = quantity;

            _eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish(barcode);

            return success;
        }

        public void CompleteSale()
		{
            var invoiceId = AddInvoiceToDatabase(_saleInvoice.InvoiceTotal, UserId, CustomerId);

            AddProductsToDatabase(_saleInvoice.Products, invoiceId);
            AddPaymentsToDatabase(_saleInvoice.Payments, invoiceId);
		}

        private int AddInvoiceToDatabase(decimal total, int userId, int? customerId)
		{
            return _invoicesRepository.AddInvoice(new DataAccess.Models.Invoice
            {
                Total = total,
                UserId = userId,
                CustomerId = customerId
            });
        }

        private void AddProductsToDatabase(IList<ISaleInvoiceProduct> products, int invoiceId)
		{
            foreach(var product in products)
			{
                AddProductToDatabase(product, invoiceId);
            }
		}

        private void AddProductToDatabase(ISaleInvoiceProduct product, int invoiceId)
        {
            _invoicesRepository.AddInvoiceProduct(new DataAccess.Models.InvoiceProduct
            {
                InvoiceId = invoiceId,
                InventoryProductId = product.InventoryProductId,
                Barcode = product.Barcode,
                Description = product.Description,
                Manufacturer = product.Manufacturer,
                Brand = product.Brand,
                Category = product.Category,
                UnitCost = product.UnitCost,
                UnitPrice = product.UnitPrice,
                Quantity = product.Quantity
            });
        }

        private void AddPaymentsToDatabase(IList<IPayment> payments, int invoiceId)
        {
            foreach(var payment in payments)
			{
                AddPaymentToDatabase(payment, invoiceId);
            }
        }

        private void AddPaymentToDatabase(IPayment payment, int invoiceId)
        {
			_invoicesRepository.AddPayment(new DataAccess.Models.Payment
            {
                InvoiceId = invoiceId,
                PaymentTypeId = payment.PaymentTypeId,
                Amount = payment.Amount
            });
        }
    }
}
