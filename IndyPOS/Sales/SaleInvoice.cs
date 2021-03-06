using System.Collections.Generic;
using System.Linq;
using IndyPOS.Enums;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Sales
{
    public class SaleInvoice : ISaleInvoice
	{
        public int? Id { get; private set; }

        public IList<ISaleInvoiceProduct> Products { get; private set; }

        public IList<IPayment> Payments { get; private set; }

        public decimal InvoiceTotal => Products.Sum(p => p.Quantity * p.UnitPrice);

        public decimal PaymentTotal => Payments.Sum(p => p.Amount);

        public decimal BalanceRemaining => InvoiceTotal - PaymentTotal;

        public bool IsRefundInvoice => InvoiceTotal < 0;

        public decimal Changes => CalculateChanges();

        public SaleInvoice()
		{
			Products = new List<ISaleInvoiceProduct>();
			Payments = new List<IPayment>();
		}

        private decimal CalculateChanges()
		{
			if (IsRefundInvoice)
				return 0m;

            var amount = PaymentTotal - InvoiceTotal;

            return amount >= 0 ? amount : 0m;
        }

		public ISaleInvoiceProduct AddProduct(InventoryProductModel product)
		{
			return AddProduct(product, product.UnitPrice, 1, string.Empty);
		}

		public ISaleInvoiceProduct AddProduct(InventoryProductModel product, decimal unitPrice, int quantity)
		{
			return AddProduct(product, unitPrice, quantity, string.Empty);
		}

		public ISaleInvoiceProduct AddProduct(InventoryProductModel product, decimal unitPrice, int quantity, string note)
		{
			var productToAdd = ConvertToSaleInvoiceProduct(product);

			productToAdd.Priority = GetNextProductPriority();
			productToAdd.UnitPrice = unitPrice;
			productToAdd.Quantity = quantity;
			productToAdd.Note = note;

			Products.Add(productToAdd);

			return Products.Last();
		}

        public void RemoveProduct(ISaleInvoiceProduct product)
		{
			Products.Remove(product);
		}

		public IPayment AddPayment(PaymentType paymentType, decimal paymentAmount, string note)
        {
			var payment = new Payment
						  {
							  PaymentTypeId = (int) paymentType,
							  Priority = GetNextPaymentPriority(),
							  Amount = paymentAmount,
							  Note = note
						  };

			Payments.Add(payment);

			return Payments.Last();
		}

        public void RemoveAllPayments()
        {
            Payments.Clear();
        }

		private int GetNextProductPriority()
		{
			return Products.Count > 0 ? Products.Max(p => p.Priority) + 1 : 1;
		}

		private int GetNextPaymentPriority()
		{
			return Payments.Count > 0 ? Payments.Max(p => p.Priority) + 1 : 1;
		}

		public void SetSaleInvoiceId(int id)
		{
			Id = id;
		}

		public void StartNewSale()
		{
			Id = null;
			Products = new List<ISaleInvoiceProduct>();
			Payments = new List<IPayment>();
		}

		private ISaleInvoiceProduct ConvertToSaleInvoiceProduct(InventoryProductModel product)
		{
			return new SaleInvoiceProduct
				   {
					   InventoryProductId = product.InventoryProductId,
					   Barcode = product.Barcode,
					   Description = product.Description,
					   Manufacturer = product.Manufacturer,
					   Brand = product.Brand,
					   Category = product.Category,
					   UnitPrice = product.UnitPrice,
					   Quantity = 1,
					   GroupPrice = product.GroupPrice,
					   GroupPriceQuantity = product.GroupPriceQuantity,
					   IsTrackable = product.IsTrackable
				   };
		}
    }
}
