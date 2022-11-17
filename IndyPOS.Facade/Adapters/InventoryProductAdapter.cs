using IndyPOS.Facade.Interfaces;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Facade.Adapters;

public class InventoryProductAdapter : IInventoryProduct
{
	private readonly InventoryProductModel _adaptee;

	public InventoryProductAdapter(InventoryProductModel adaptee)
	{
		_adaptee = adaptee;
	}

	public int InventoryProductId => _adaptee.InventoryProductId;

	public string Barcode
	{
		get => _adaptee.Barcode;
		set => _adaptee.Barcode = value;
	}

	public string Description
	{
		get => _adaptee.Description;
		set => _adaptee.Description = value;
	}

	public string Manufacturer
	{
		get => _adaptee.Manufacturer;
		set => _adaptee.Manufacturer = value;
	}

	public string Brand
	{
		get => _adaptee.Brand;
		set => _adaptee.Brand = value;
	}

	public int Category
	{
		get => _adaptee.Category;
		set => _adaptee.Category = value;
	}

	public decimal UnitPrice
	{
		get => _adaptee.UnitPrice;
		set => _adaptee.UnitPrice = value;
	}

	public int QuantityInStock
	{
		get => _adaptee.QuantityInStock;
		set => _adaptee.QuantityInStock = value;
	}

	public decimal? GroupPrice
	{
		get => _adaptee.GroupPrice;
		set => _adaptee.GroupPrice = value;
	}

	public int? GroupPriceQuantity
	{
		get => _adaptee.GroupPriceQuantity;
		set => _adaptee.GroupPriceQuantity = value;
	}

	public bool IsTrackable
	{
		get => _adaptee.IsTrackable;
		set => _adaptee.IsTrackable = value;
	}
	public string DateCreated => _adaptee.DateCreated;

	public string DateUpdated => _adaptee.DateUpdated;
}