using System.Collections.Generic;
using IndyPOS.Enums;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Sales
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

		int GetNextProductPriority();

		int GetNextPaymentPriority();

		void AddProduct(InventoryProductModel product);

		void AddProduct(InventoryProductModel product, decimal unitPrice, int quantity);

		void RemoveProduct(ISaleInvoiceProduct product);

		void AddPayment(PaymentType paymentType, decimal paymentAmount, string note);

		void RemoveAllPayments();

        void SetSaleInvoiceId(int id);

		ISaleInvoiceProduct GetProductByInventoryProductIdAndPriority(int inventoryProductId, int priority);
	}
}