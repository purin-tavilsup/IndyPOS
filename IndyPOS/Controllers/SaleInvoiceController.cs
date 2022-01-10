using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Devices;
using IndyPOS.Enums;
using IndyPOS.Events;
using IndyPOS.Extensions;
using IndyPOS.Inventory;
using IndyPOS.Sales;
using IndyPOS.Users;
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
		private readonly IReceiptPrinter _receiptPrinter;
		private readonly IUserAccountHelper _userAccountHelper;
        private ISaleInvoice _saleInvoice;

        public IReadOnlyCollection<ISaleInvoiceProduct> Products => (IReadOnlyCollection<ISaleInvoiceProduct>)_saleInvoice.Products;

        public IReadOnlyCollection<IPayment> Payments => (IReadOnlyCollection<IPayment>)_saleInvoice.Payments;

        public decimal InvoiceTotal => _saleInvoice.InvoiceTotal;

        public decimal PaymentTotal => _saleInvoice.PaymentTotal;

		public decimal BalanceRemaining => _saleInvoice.BalanceRemaining;

		public bool IsRefundInvoice => _saleInvoice.IsRefundInvoice;

		public bool IsPendingPayment => IsRefundInvoice
											? _saleInvoice.InvoiceTotal != _saleInvoice.PaymentTotal
											: _saleInvoice.InvoiceTotal > _saleInvoice.PaymentTotal;

        public decimal Changes => _saleInvoice.Changes;

        public SaleInvoiceController(IEventAggregator eventAggregator, 
									 IInvoiceRepository invoicesRepository, 
									 IInventoryProductRepository inventoryProductsRepository,
									 IReceiptPrinter receiptPrinter,
									 IUserAccountHelper userAccountHelper)
        {
            _eventAggregator = eventAggregator;
            _invoicesRepository = invoicesRepository;
            _inventoryProductsRepository = inventoryProductsRepository;
			_receiptPrinter = receiptPrinter;
			_userAccountHelper = userAccountHelper;
		}
		
        public void StartNewSale()
        {
            _saleInvoice = new SaleInvoice();

            _eventAggregator.GetEvent<NewSaleStartedEvent>().Publish();
        }

        public void RemoveAllPayments()
		{
			_saleInvoice.Payments.Clear();

            _eventAggregator.GetEvent<AllPaymentsRemovedEvent>().Publish();
		}

		public bool AddProduct(string barcode)
        { 
            var product = GetInventoryProductByBarcode(barcode);

            if (product == null)
                return false;

            var invoiceProduct = product.ToSaleInvoiceProduct();

            invoiceProduct.Priority = GetNextProductPriority(_saleInvoice.Products);
			invoiceProduct.Quantity = 1;

            _saleInvoice.Products.Add(invoiceProduct);

            _eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish();

			return true;
		}

        private int GetNextProductPriority(ICollection<ISaleInvoiceProduct> products)
		{
            return products.Count > 0 ? products.Max(p => p.Priority) + 1 : 1;
        }

        private int GetNextPaymentPriority(ICollection<IPayment> payments)
        {
            return payments.Count > 0 ? payments.Max(p => p.Priority) + 1 : 1;
        }

        public void RemoveProduct(ISaleInvoiceProduct product)
        {
            _saleInvoice.Products.Remove(product);

            _eventAggregator.GetEvent<SaleInvoiceProductRemovedEvent>().Publish();
        }

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
        {
            var result = _inventoryProductsRepository.GetProductByBarcode(barcode);

            return result != null ? new InventoryProductAdapter(result) : null;
        }

		private IInventoryProduct GetInventoryProductById(int id)
		{
			var result = _inventoryProductsRepository.GetProductById(id);

			return result != null ? new InventoryProductAdapter(result) : null;
		}

        public void AddPayment(PaymentType paymentType, decimal paymentAmount, string note)
		{
            var payment = new Payment
			{
				PaymentTypeId = (int)paymentType,
                Priority = GetNextPaymentPriority(_saleInvoice.Payments),
                Amount = paymentAmount,
				Note = note
			};

            _saleInvoice.Payments.Add(payment);

            _eventAggregator.GetEvent<PaymentAddedEvent>().Publish();
        }

        public void UpdateProductQuantity(int inventoryProductId, int priority, int quantity)
		{
            var productToUpdate = _saleInvoice.Products.FirstOrDefault(p => p.InventoryProductId == inventoryProductId &&
																			p.Priority == priority);

            if (productToUpdate == null)
                return;

			if (productToUpdate.Quantity == quantity)
				return;

			if (productToUpdate.Quantity < quantity)
			{
				IncreaseProductQuantity(productToUpdate, quantity);
			}
			else
			{
				DecreaseProductQuantity(productToUpdate, quantity);
			}
        }

		public void UpdateProductUnitPrice(int inventoryProductId, int priority, decimal unitPrice, string note)
        {
			var productToUpdate = _saleInvoice.Products.FirstOrDefault(p => p.InventoryProductId == inventoryProductId &&
																			p.Priority == priority);

			if (productToUpdate == null)
				return;

			if (productToUpdate.UnitPrice == unitPrice)
				return;

			productToUpdate.UnitPrice = unitPrice;
			productToUpdate.Note = note;

			_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();
		}

		private void IncreaseProductQuantity(ISaleInvoiceProduct product, int quantity)
		{
			if (!product.GroupPriceQuantity.HasValue && !product.GroupPrice.HasValue ||
				product.GroupPriceQuantity.HasValue && quantity < product.GroupPriceQuantity.Value)
			{
				product.Quantity = quantity;

				_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();

				return;
			}

			var groupPrice = product.GroupPrice.GetValueOrDefault();
			var groupPriceQuantity = product.GroupPriceQuantity.GetValueOrDefault();

            // Update base product
			product.Quantity = groupPriceQuantity;
			product.UnitPrice = groupPrice / groupPriceQuantity;

			_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();

			var remainingQuantity = quantity - groupPriceQuantity;
			var productId = product.InventoryProductId;

            // Add new line products for remaining quantity
			while (remainingQuantity > 0)
			{
				var quantityToAdd = remainingQuantity > groupPriceQuantity 
										? groupPriceQuantity 
										: remainingQuantity;

				AddProductInternal(productId, quantityToAdd);

				remainingQuantity -= groupPriceQuantity;
			}
		}

		private void AddProductInternal(int inventoryProductId, int quantity)
		{ 
			var product = GetInventoryProductById(inventoryProductId);

			if (product == null)
				return;

			var invoiceProduct = product.ToSaleInvoiceProduct();
			var unitPrice = invoiceProduct.GroupPrice.HasValue && invoiceProduct.GroupPriceQuantity == quantity
								? invoiceProduct.GroupPrice.Value / invoiceProduct.GroupPriceQuantity.Value
								: invoiceProduct.UnitPrice;

			invoiceProduct.Priority = GetNextProductPriority(_saleInvoice.Products);
			invoiceProduct.Quantity = quantity;
			invoiceProduct.UnitPrice = unitPrice;

			_saleInvoice.Products.Add(invoiceProduct);

			_eventAggregator.GetEvent<SaleInvoiceProductAddedEvent>().Publish();
		}

		private void DecreaseProductQuantity(ISaleInvoiceProduct product, int quantity)
		{
			if (product.GroupPriceQuantity.HasValue &&
				product.GroupPrice.HasValue &&
				product.Quantity == product.GroupPriceQuantity)
			{
				product.Quantity = quantity;

				var inventoryProduct = _inventoryProductsRepository.GetProductById(product.InventoryProductId);

				// Restore original unit price
				product.UnitPrice = inventoryProduct.UnitPrice;

				_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();

				return;
			}
            
			product.Quantity = quantity;

			_eventAggregator.GetEvent<SaleInvoiceProductUpdatedEvent>().Publish();
		}

        public IList<string> ValidateSaleInvoice()
		{
			var message = new List<string>();

			if (_userAccountHelper.LoggedInUser == null)
				message.Add("ไม่พบผู้ใช้งานในระบบ");

            if (!_saleInvoice.Products.Any()) 
				message.Add("ไม่มีรายการสินค้า");

			if (!_saleInvoice.Payments.Any()) 
				message.Add("ไม่มีรายการเงิน");

            if (IsPendingPayment)
                message.Add("รายการเงินยังไม่สมบูรณ์");

			return message;
		}

		public void CompleteSale()
		{
			var loggedInUserId = _userAccountHelper.LoggedInUser.UserId;

            AddInvoiceToDatabase(_saleInvoice, loggedInUserId);
			AddInvoiceProductsToDatabase(_saleInvoice);
            AddPaymentsToDatabase(_saleInvoice);
			UpdateInventoryProductsSoldOnInvoice(_saleInvoice);
		}

		public void PrintReceipt()
		{
			var loggedInUser = _userAccountHelper.LoggedInUser;

			_receiptPrinter.PrintReceipt(_saleInvoice, loggedInUser);
		}

        private void UpdateInventoryProductsSoldOnInvoice(ISaleInvoice saleInvoice)
		{
			var productGroups = saleInvoice.Products
										   .Where(p => p.IsTrackable)
										   .GroupBy(p => p.InventoryProductId);

			foreach (var group in productGroups)
			{
				var id = group.Key;
				var quantity = group.Sum(p => p.Quantity);
				var product = _inventoryProductsRepository.GetProductById(id);
				var newQuantity = product.QuantityInStock - quantity;

                _inventoryProductsRepository.UpdateProductQuantityById(id, newQuantity);
			}
		}

		private void AddInvoiceToDatabase(ISaleInvoice saleInvoice, int userId)
		{
            var  invoiceId = _invoicesRepository.AddInvoice(new DataAccess.Models.Invoice 
															{ 
																Total = saleInvoice.InvoiceTotal,
																UserId = userId,
																CustomerId = null
															});

			saleInvoice.SetSaleInvoiceId(invoiceId);
        }

        private void AddInvoiceProductsToDatabase(ISaleInvoice saleInvoice)
		{
            foreach(var product in saleInvoice.Products)
			{
                AddProductToDatabase(product, saleInvoice.Id.GetValueOrDefault());
            }
		}

        private void AddProductToDatabase(ISaleInvoiceProduct product, int invoiceId)
        {
            _invoicesRepository.AddInvoiceProduct(new DataAccess.Models.InvoiceProduct
            {
                InvoiceId = invoiceId,
                InventoryProductId = product.InventoryProductId,
				Priority = product.Priority,
                Barcode = product.Barcode,
                Description = product.Description,
                Manufacturer = product.Manufacturer,
                Brand = product.Brand,
                Category = product.Category,
                UnitPrice = product.UnitPrice,
                Quantity = product.Quantity,
				Note = product.Note
            });
        }

        private void AddPaymentsToDatabase(ISaleInvoice saleInvoice)
        {
            foreach(var payment in saleInvoice.Payments)
			{
                AddPaymentToDatabase(payment, saleInvoice.Id.GetValueOrDefault());
            }
        }

        private void AddPaymentToDatabase(IPayment payment, int invoiceId)
        {
			_invoicesRepository.AddPayment(new DataAccess.Models.Payment
										   {
											   InvoiceId = invoiceId,
											   PaymentTypeId = payment.PaymentTypeId,
											   Amount = payment.Amount,
											   Note = payment.Note
										   });
		}
    }
}
