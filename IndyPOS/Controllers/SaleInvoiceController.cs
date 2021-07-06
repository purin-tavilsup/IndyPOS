using IndyPOS.Adapters;
using IndyPOS.DataServices;
using IndyPOS.Enums;
using IndyPOS.Events;
using IndyPOS.Extensions;
using IndyPOS.SaleInvoice;
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

        public IList<IPayment> Payments { get; }

        public decimal InvoiceTotal => Products.Sum(p => p.Quantity * p.UnitPrice);

        public decimal PaymentTotal => Payments.Sum(p => p.Amount);

        public decimal Changes => CalculateChanges();

        public SaleInvoiceController(IEventAggregator eventAggregator, 
            IInvoicesDataService invoicesDataService, 
            IInventoryProductsDataService inventoryProductsDataService)
        {
            _eventAggregator = eventAggregator;
            _invoicesDataService = invoicesDataService;
            _inventoryProductsDataService = inventoryProductsDataService;
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

        public bool AddPaymentToSaleInvoice(PaymentType paymentType, decimal paymentAmount)
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
    }
}
