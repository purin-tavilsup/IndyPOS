using IndyPOS.Adapters;
using IndyPOS.Constants;
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

        public void RemoveAllPayments()
		{
			_saleInvoice.Payments.Clear();

            _eventAggregator.GetEvent<AllPaymentsRemovedEvent>().Publish();
		}

        public bool AddProduct(string barcode)
        { 
			var success = true;
            var product = GetInventoryProductByBarcode(barcode);

            if (product == null)
                return !success;

            var invoiceProduct = product.ToSaleInvoiceProduct();

            invoiceProduct.Priority = GetNextProductPriority(_saleInvoice.Products);

            _saleInvoice.Products.Add(invoiceProduct);

            _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish(barcode);

            return success;
        }

        private int GetNextProductPriority(IList<ISaleInvoiceProduct> products)
		{
            return products.Count > 0 ? products.Max(p => p.Priority) + 1 : 1;
        }

        private int GetNextPaymentPriority(IList<IPayment> payments)
        {
            return payments.Count > 0 ? payments.Max(p => p.Priority) + 1 : 1;
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
                Priority = GetNextPaymentPriority(_saleInvoice.Payments),
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

        public IList<string> ValidateSaleInvoice()
		{
			var message = new List<string>();

            if (!_saleInvoice.Products.Any()) 
				message.Add("ไม่มีสินค้าในรายการ");

            if (InvoiceTotal > PaymentTotal)
                message.Add("ค้างค่าชำระสินค้า");

			return message;
		}

		public void CompleteSale()
		{
            var invoiceId = AddInvoiceToDatabase(_saleInvoice.InvoiceTotal, UserId, CustomerId);
            AddInvoiceProductsToDatabase(_saleInvoice.Products, invoiceId);
            AddPaymentsToDatabase(_saleInvoice.Payments, invoiceId);
			UpdateInventoryProductsSoldOnInvoice(_saleInvoice.Products);
		}

        private void UpdateInventoryProductsSoldOnInvoice(IList<ISaleInvoiceProduct> products)
		{
			var productGroups = products.GroupBy(p => p.InventoryProductId);

			foreach (var group in productGroups)
			{
				var id = group.Key;
				var quantity = group.Sum(p => p.Quantity);
				var product = _inventoryProductsRepository.GetProductById(id);
				var newQuantity = product.QuantityInStock - quantity;

                _inventoryProductsRepository.UpdateProductQuantityById(id, newQuantity);
			}
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

        private void AddInvoiceProductsToDatabase(IList<ISaleInvoiceProduct> products, int invoiceId)
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
