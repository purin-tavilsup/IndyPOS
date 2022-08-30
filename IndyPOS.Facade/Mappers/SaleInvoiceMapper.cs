using IndyPOS.Common.Enums;
using IndyPOS.Common.Interfaces;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using Payment = IndyPOS.Facade.Models.Report.Payment;

namespace IndyPOS.Facade.Mappers
{
	public class SaleInvoiceMapper : ISaleInvoiceMapper
	{
		private readonly IReadOnlyDictionary<int, string> _productCategories;
		private readonly IReadOnlyDictionary<int, string> _paymentTypes;

        public SaleInvoiceMapper(IStoreConstants storeConstants)
		{
			_productCategories = storeConstants.ProductCategories;
			_paymentTypes = storeConstants.PaymentTypes;
		}

		public Invoice Map(ISaleInvoice invoice)
		{
			var id = invoice.Id.HasValue ? invoice.Id.ToString() : Guid.NewGuid().ToString();

			return new Invoice
			{
				Id = id,
				Date = DateTime.Now.ToString("yyyy-M-d"),
				DateTime = DateTime.UtcNow,
				Products = MapProducts(invoice.Products),
				Payments = MapPayments(invoice.Payments)
			};
		}

		private Product[] MapProducts(IEnumerable<ISaleInvoiceProduct> invoiceProducts)
		{
			return invoiceProducts.Select(MapProduct)
								  .ToArray();
		}

		private Payment[] MapPayments(IEnumerable<IPayment> invoicePayments)
		{
			return invoicePayments.Select(MapPayment)
								  .ToArray();
		}

		private Product MapProduct(ISaleInvoiceProduct invoiceProduct)
		{
			return new Product
			{
				Priority = invoiceProduct.Priority,
				Description = invoiceProduct.Description,
				Barcode = invoiceProduct.Barcode,
				Category = _productCategories[invoiceProduct.Category],
				Group = invoiceProduct.Category < (int) ProductCategory.Hardware ? "General" : "Hardware",
				UnitPrice = (double) invoiceProduct.UnitPrice,
				Quantity = invoiceProduct.Quantity,
				Note = invoiceProduct.Note,
				DateTime = DateTime.UtcNow
			};
		}

		private Payment MapPayment(IPayment invoicePayment)
		{
			return new Payment
			{
				Priority = invoicePayment.Priority,
				PaymentType = _paymentTypes[invoicePayment.PaymentTypeId],
				Amount = (double) invoicePayment.Amount,
				Note = invoicePayment.Note,
				DateTime = DateTime.UtcNow
			};
		}
    }
}
