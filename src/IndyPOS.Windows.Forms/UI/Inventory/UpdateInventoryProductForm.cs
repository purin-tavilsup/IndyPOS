using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Domain.Enums;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.InventoryProducts;

namespace IndyPOS.Windows.Forms.UI.Inventory;

[ExcludeFromCodeCoverage]
public partial class UpdateInventoryProductForm : Form
{
	private readonly IInventoryProductService _inventoryProductService;
	private readonly IStoreHubClient _storeHubClient;
	private readonly MessageForm _messageForm;
	private InventoryProductDto? _product;

	/// <summary>The store's catalogue, fetched when the dialog opens. The combo shows DisplayName;
	/// the server expects Code, so every save looks the code back up from here.</summary>
	private IReadOnlyList<ProductCategoryDto> _categories = [];

	public UpdateInventoryProductForm(MessageForm messageForm,
									  IInventoryProductService inventoryProductService,
									  IStoreHubClient storeHubClient)
	{
		_messageForm = messageForm;
		_inventoryProductService = inventoryProductService;
		_storeHubClient = storeHubClient;
		_product = null;

		InitializeComponent();
	}

	public async Task ShowDialog(InventoryProductDto product)
	{
		_product = product;

		BarcodeTextBox.Texts = _product.Barcode;

		await PopulateProductCategoryComboBoxAsync();
		PopulateProductProperties();

		RemoveProductButton.Enabled = product.IsTrackable;

		base.ShowDialog();
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
		// An existing product may sit in a category this store no longer offers; show its label
		// rather than a blank, so editing an unrelated field does not silently retype it.
		CategoryComboBox.Texts =
			_categories.FirstOrDefault(c => c.Code == _product.Category)?.DisplayName ?? string.Empty;
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

		if (SelectedCategoryCode() is null)
		{
			_messageForm.ShowDialog("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
			return false;
		}

		return true;
	}

	private async Task PopulateProductCategoryComboBoxAsync()
	{
		CategoryComboBox.Items.Clear();
		_categories = [];

		IReadOnlyList<ProductCategoryDto> categories;
		bool multipleTypes;
		try
		{
			categories = await _storeHubClient.GetProductCategoriesAsync();
			multipleTypes = (await _storeHubClient.GetStoreFeaturesAsync()).MultipleProductTypesEnabled;
		}
		catch
		{
			// The server still guards updates, so a fetch failure must not offer a wrong set.
			// Leave the picker empty and let the operator retry.
			return;
		}

		_categories = categories;

		foreach (var category in categories.Where(c => c.IsEnabled))
		{
			// Hide kinds this store may not use; the server rejects them anyway.
			if (!multipleTypes && category.Kind != ProductCategoryKind.GeneralGoods)
				continue;

			CategoryComboBox.Items.Add(category.DisplayName);
		}
	}

	/// <summary>The catalogue Code behind the selected DisplayName, or null if nothing matches.</summary>
	private string? SelectedCategoryCode() =>
		_categories.FirstOrDefault(c => c.DisplayName == CategoryComboBox.Texts.Trim())?.Code;

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
		// Validated before we get here, so a null code is unreachable.
		var categoryCode = SelectedCategoryCode()!;

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
			Category = categoryCode,
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