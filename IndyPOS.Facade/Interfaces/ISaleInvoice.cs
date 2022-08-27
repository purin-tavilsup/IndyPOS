using System.Collections.Generic;
using IndyPOS.Common.Enums;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Facade.Interfaces
{
	public interface ISaleInvoice
	{
        int? Id { get; }

		IList<ISaleInvoiceProduct> Products { get; }

        IList<IPayment> Payments { get; }

        decimal InvoiceTotal { get; }

        decimal PaymentTotal { get; }

		decimal BalanceRemaining { get; }

		bool IsRefundInvoice { get; }

        decimal Changes { get; }

		void StartNewSale();

		ISaleInvoiceProduct AddProduct(InventoryProductModel product);

		ISaleInvoiceProduct AddProduct(InventoryProductModel product, decimal unitPrice, int quantity);

		ISaleInvoiceProduct AddProduct(InventoryProductModel product, decimal unitPrice, int quantity, string note);

		void RemoveProduct(ISaleInvoiceProduct product);

		IPayment AddPayment(PaymentType paymentType, decimal paymentAmount, string note);

		void RemoveAllPayments();

		void SetSaleInvoiceId(int id);
	}
}