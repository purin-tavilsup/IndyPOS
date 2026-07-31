using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Inventory;

[ExcludeFromCodeCoverage]
public partial class AddNewInventoryProductForm : Form
{
	private readonly IInventoryProductService _inventoryProductService;
	private readonly IStoreHubClient _storeHubClient;
	private readonly MessageForm _messageForm;

	private readonly ProductCategoryPicker _categoryPicker;

	public AddNewInventoryProductForm(MessageForm messageForm,
									  IInventoryProductService inventoryProductService,
									  IStoreHubClient storeHubClient)
	{
		_messageForm = messageForm;
		_inventoryProductService = inventoryProductService;
		_storeHubClient = storeHubClient;
		_categoryPicker = new ProductCategoryPicker(storeHubClient);

		InitializeComponent();
	}

	public async Task ShowDialog(string? productBarcode = null)
	{
		if (!await PopulateProductCategoryComboBoxAsync())
		{
			// Opening a form whose category picker is empty just means the operator fills it in
			// and is then told to "select a valid category" — which is not the real problem.
			_messageForm.ShowDialog("ไม่สามารถโหลดประเภทสินค้าได้ กรุณาลองใหม่อีกครั้ง",
									"ไม่สามารถโหลดประเภทสินค้าได้");
			return;
		}

		ResetProductEntry();

		if (string.IsNullOrWhiteSpace(productBarcode))
		{
			ProductCodeTextBox.ReadOnly = false;
		}
		else
		{
			ProductCodeTextBox.Texts = productBarcode;
			ProductCodeTextBox.ReadOnly = true;
		}

		base.ShowDialog();
	}

	private void ResetProductEntry()
	{
		ProductCodeTextBox.Texts = string.Empty;
		DescriptionTextBox.Texts = string.Empty;
		QuantityTextBox.Texts = string.Empty;
		UnitPriceTextBox.Texts = string.Empty;
		CategoryComboBox.Texts = "เลือกประเภทสินค้า";
		GroupPriceTextBox.Texts = string.Empty;
		GroupPriceQuantityTextBox.Texts = string.Empty;
		ManufacturerTextBox.Texts = string.Empty;
		BrandTextBox.Texts = string.Empty;
		IsTrackableCheckBox.Checked = true;
	}

	private bool ValidateProductEntry()
	{
		if (string.IsNullOrWhiteSpace(ProductCodeTextBox.Texts))
		{
			_messageForm.ShowDialog("กรุณาใส่รหัสสินค้าหรือบาร์โค้ดให้ถูกต้อง", "รหัสสินค้าไม่ถูกต้อง");
                
			return false;
		}
                
		if (string.IsNullOrWhiteSpace(DescriptionTextBox.Texts))
		{
			_messageForm.ShowDialog("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
                
			return false;
		}

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

	private async Task<bool> PopulateProductCategoryComboBoxAsync()
	{
		CategoryComboBox.Items.Clear();

		if (!await _categoryPicker.LoadAsync())
			return false;

		foreach (var category in _categoryPicker.Selectable)
		{
			CategoryComboBox.Items.Add(category.DisplayName);
		}

		return true;
	}

	private string? SelectedCategoryCode() => _categoryPicker.CodeFor(CategoryComboBox.Texts);

	private async void SaveProductEntryButton_Click(object sender, EventArgs e)
	{
		if (!ValidateProductEntry())
			return;

		try
		{
			var request = CreateRequestForCreateProduct();

			await _inventoryProductService.CreateAsync(request);

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า");
		}
	}

	private CreateInventoryProductRequest CreateRequestForCreateProduct()
	{
		// Required Attributes
		var quantity = int.Parse(QuantityTextBox.Texts.Trim());
		var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
		// Validated before we get here, so a null code is unreachable.
		var categoryCode = SelectedCategoryCode()!;

		// Optional Attributes
		decimal? groupPrice = decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var gp) ? gp : null;
		int? groupPriceQuantity = int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var gpq) ? gpq : null;

		return new CreateInventoryProductRequest
		{
			Barcode = ProductCodeTextBox.Texts.Trim(),
			Description = DescriptionTextBox.Texts.Trim(),
			QuantityInStock = quantity,
			UnitPrice = unitPrice,
			Category = categoryCode,
			IsTrackable = IsTrackableCheckBox.Checked,
			Manufacturer = string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts) ? null : ManufacturerTextBox.Texts.Trim(),
			Brand = string.IsNullOrWhiteSpace(BrandTextBox.Texts) ? null : BrandTextBox.Texts.Trim(),
			GroupPrice = groupPrice,
			GroupPriceQuantity = groupPriceQuantity
		};
	}

	private void CancelProductEntryButton_Click(object sender, EventArgs e)
	{
		Close();
	}

	private void IsTrackableCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		if (!IsTrackableCheckBox.Checked)
		{
			QuantityTextBox.Texts = "1";
		}

		QuantityTextBox.Enabled = IsTrackableCheckBox.Checked;
	}
}