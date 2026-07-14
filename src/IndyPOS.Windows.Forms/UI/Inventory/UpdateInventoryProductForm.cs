using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.InventoryProducts;

namespace IndyPOS.Windows.Forms.UI.Inventory;

[ExcludeFromCodeCoverage]
public partial class UpdateInventoryProductForm : Form
{
	private readonly IInventoryProductService _inventoryProductService;
	private readonly MessageForm _messageForm;
	private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
	private InventoryProductDto? _product;

	public UpdateInventoryProductForm(IStoreConstants storeConstants,
									  MessageForm messageForm,
									  IInventoryProductService inventoryProductService)
	{
		_productCategoryDictionary = storeConstants.ProductCategories;
		_messageForm = messageForm;
		_inventoryProductService = inventoryProductService;
		_product = null;

		InitializeComponent();
		InitializeProductCategories();
	}

	public void ShowDialog(InventoryProductDto product)
	{
		_product = product;

		BarcodeTextBox.Texts = _product.Barcode;

		PopulateProductProperties();

		RemoveProductButton.Enabled = product.IsTrackable;

		ShowDialog();
	}

	private void PopulateProductProperties()
	{
		if (_product is null)
		{
			return;
		}
		
		DescriptionTextBox.Texts = _product.Description;
		QuantityLabel.Text = $"{_product.QuantityInStock}";
		UnitPriceTextBox.Texts = $"{_product.UnitPrice:N}";
		CategoryComboBox.Texts = _productCategoryDictionary[_product.Category];
		GroupPriceTextBox.Texts = $"{_product.GroupPrice:N}";
		GroupPriceQuantityTextBox.Texts = _product.GroupPriceQuantity.HasValue ? $"{_product.GroupPriceQuantity.Value}" : string.Empty;
		ManufacturerTextBox.Texts = _product.Manufacturer;
		BrandTextBox.Texts = _product.Brand;
	}

	private bool ValidateProductEntry()
	{
		if (string.IsNullOrWhiteSpace(DescriptionTextBox.Texts))
		{
			_messageForm.ShowDialog("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
			return false;
		}

		if (decimal.TryParse(UnitPriceTextBox.Texts.Trim(), out var unitPrice))
		{
			if (unitPrice < 0m)
			{
				_messageForm.ShowDialog("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
				return false;
			}
		}
		else
		{
			_messageForm.ShowDialog("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
			return false;
		}

		if (!_productCategoryDictionary.Values.Contains(CategoryComboBox.Texts.Trim()))
		{
			_messageForm.ShowDialog("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
			return false;
		}

		return true;
	}

	private void InitializeProductCategories()
	{
		CategoryComboBox.Items.Clear();

		foreach (var item in _productCategoryDictionary)
		{
			CategoryComboBox.Items.Add(item.Value);
		}
	}

	private async void UpdateProductButton_Click(object sender, EventArgs e)
	{
		if (_product is null || !ValidateProductEntry())
			return;

		try
		{
			var request = CreateRequestForUpdateProduct(_product);

			await _inventoryProductService.UpdateAsync(request);

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า");
		}
	}

	private UpdateInventoryProductRequest CreateRequestForUpdateProduct(InventoryProductDto product)
	{
		var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Texts);
		var categoryId = category.Key;

		// Optional Attributes
		decimal? groupPrice = decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var gp) ? gp : null;
		int? groupPriceQuantity = int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var gpq) ? gpq : null;

		return new UpdateInventoryProductRequest
		{
			Id = product.Id,
			Description = DescriptionTextBox.Texts.Trim(),
			QuantityInStock = int.Parse(QuantityLabel.Text.Trim()),
			UnitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim()),
			GroupPrice = groupPrice,
			GroupPriceQuantity = groupPriceQuantity,
			Category = categoryId,
			Manufacturer = string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts) ? null : ManufacturerTextBox.Texts.Trim(),
			Brand = string.IsNullOrWhiteSpace(BrandTextBox.Texts) ? null : BrandTextBox.Texts.Trim()
		};
	}

	private void CancelUpdateProductButton_Click(object sender, EventArgs e)
	{
		Close();
	}

	private async void RemoveProductButton_Click(object sender, EventArgs e)
	{
		if (_product is null)
		{
			return;
		}

		try
		{
			await _inventoryProductService.DeleteAsync(_product.Id);

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังลบสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังลบสินค้า");
		}
	}

	private void IncreaseQuantityButton_Click(object sender, EventArgs e)
	{
		var amount = int.Parse(QuantityTextBox.Texts.Trim());
		var quantity = int.Parse(QuantityLabel.Text.Trim());

		QuantityLabel.Text = $"{quantity + amount}";

		QuantityTextBox.Texts = string.Empty;
	}

	private void DecreaseQuantityButton_Click(object sender, EventArgs e)
	{
		if (!ValidateQuantity())
			return;

		var amount = int.Parse(QuantityTextBox.Texts.Trim());
		var quantity = int.Parse(QuantityLabel.Text.Trim());

		QuantityLabel.Text = $"{quantity - amount}";

		QuantityTextBox.Texts = string.Empty;
	}

	private bool ValidateQuantity()
	{
		if(int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity))
		{
			if (quantity < 1)
			{
				_messageForm.ShowDialog("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
				return false;
			}
		}
		else
		{
			_messageForm.ShowDialog("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
			return false;
		}

		return true;
	}
}