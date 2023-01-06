namespace IndyPOS.Application.Common.Interfaces;

public interface IFinalInvoiceProduct
{
	int InvoiceProductId { get; }

	int Priority { get; }

	int InvoiceId { get; }

	int InventoryProductId { get; }

	string Barcode { get; }

	string Description { get; }

	string Manufacturer { get; }

	string Brand { get; }

	int Category { get; }

	decimal UnitPrice { get; }

	int Quantity { get; }

	bool IsTrackable { get; }

	string DateCreated { get; }

	string Note { get;  }
}