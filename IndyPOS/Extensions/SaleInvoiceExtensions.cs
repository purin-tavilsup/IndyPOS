using IndyPOS.Enums;
using IndyPOS.Facade.Models;
using IndyPOS.Sales;
using System.Linq;

namespace IndyPOS.Extensions
{
	public static class SaleInvoiceExtensions
    {
		public static SalesSummary CreateSalesSummary(this ISaleInvoice invoice)
		{
			var generalProductsTotal = 0m;
			var hardwareProductsTotal = 0m;
			var invoiceProducts = invoice.Products;

			foreach (var product in invoiceProducts)
			{
				var productTotal = product.UnitPrice * product.Quantity;

				if (product.Category < (int) ProductCategory.Hardware)
				{
					generalProductsTotal += productTotal;
				}
				else
				{
					hardwareProductsTotal += productTotal;
				}
			}

			var hasArPayment = HasArPayment(invoice);
			var invoiceTotal = invoice.InvoiceTotal;
			var arTotalForGeneralProducts = hasArPayment ? generalProductsTotal : 0m;
			var arTotalForHardwareProducts = hasArPayment ? hardwareProductsTotal : 0m;
			var arTotal = hasArPayment ? arTotalForGeneralProducts + arTotalForHardwareProducts : 0m;
			var invoiceTotalWithoutAr = hasArPayment ? 0m : invoiceTotal;
			var generalProductsTotalWithoutAr = hasArPayment ? 0m : generalProductsTotal;
			var hardwareProductsTotalWithoutAr = hasArPayment ? 0m : hardwareProductsTotal;

			var summary = new SalesSummary
			{
				InvoiceTotal = (double) invoiceTotal,
				GeneralProductsTotal = (double) generalProductsTotal,
				HardwareProductsTotal = (double) hardwareProductsTotal,
				ArTotal = (double) arTotal,
				ArTotalForGeneralProducts = (double) arTotalForGeneralProducts,
				ArTotalForHardwareProducts = (double) arTotalForHardwareProducts,
				InvoiceTotalWithoutAr = (double) invoiceTotalWithoutAr,
				GeneralProductsTotalWithoutAr = (double) generalProductsTotalWithoutAr,
				HardwareProductsTotalWithoutAr = (double) hardwareProductsTotalWithoutAr
			};

			return summary;
		}

		private static bool HasArPayment(ISaleInvoice invoice)
		{
			return invoice.Payments.Any(p => p.PaymentTypeId == (int) PaymentType.AccountReceivable);
		}
    }
}
