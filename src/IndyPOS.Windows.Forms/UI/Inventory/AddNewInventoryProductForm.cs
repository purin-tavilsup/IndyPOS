using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.InventoryProducts.Commands.CreateInventoryProduct;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Inventory;

[ExcludeFromCodeCoverage]
public partial class AddNewInventoryProductForm : Form
{
	private readonly IMediator _mediator;
	private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
	private readonly MessageForm _messageForm;

	public AddNewInventoryProductForm(IStoreConstants storeConstants,
									  IMediator mediator,
									  MessageForm messageForm)
	{
		_mediator = mediator;
		_productCategoryDictionary = storeConstants.ProductCategories;
		_messageForm = messageForm;

		InitializeComponent();
		InitializeProductCategories();
	}

	public void ShowDialog(string? productBarcode = null)
	{
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
			_messageForm.Show("กรุณาใส่รหัสสินค้าหรือบาร์โค้ดให้ถูกต้อง", "รหัสสินค้าไม่ถูกต้อง");
                
			return false;
		}
                
		if (string.IsNullOrWhiteSpace(DescriptionTextBox.Texts))
		{
			_messageForm.Show("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
                
			return false;
		}

		if(int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity))
		{
			if (quantity < 1)
			{
				_messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");

				return false;
			}
		}
		else
		{
			_messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");

			return false;
		}

		if (decimal.TryParse(UnitPriceTextBox.Texts.Trim(), out var unitPrice))
		{
			if (unitPrice < 0m)
			{
				_messageForm.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");

				return false;
			}
		}
		else
		{
			_messageForm.Show("กรุณาใส่ราคาขายให้ถูกต้อง", "ราคาขายไม่ถูกต้อง");
                
			return false;
		}

		if (!_productCategoryDictionary.Values.Contains(CategoryComboBox.Texts.Trim()))
		{
			_messageForm.Show("กรุณาเลือกประเภทสินค้าให้ถูกต้อง", "ประเภทสินค้าไม่ถูกต้อง");
                
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

	private async void SaveProductEntryButton_Click(object sender, EventArgs e)
	{
		if (!ValidateProductEntry())
			return;

		try
		{
			var command = CreateCommandForCreateProduct();

			await _mediator.Send(command);

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังบันทึกสินค้า");
		}
	}

	private CreateInventoryProductCommand CreateCommandForCreateProduct()
	{
		// Required Attributes
		var quantity = int.Parse(QuantityTextBox.Texts.Trim());
		var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
		var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Texts);
		var categoryId = category.Key;

		var command = new CreateInventoryProductCommand()
		{
			Barcode = ProductCodeTextBox.Texts.Trim(),
			Description = DescriptionTextBox.Texts.Trim(),
			QuantityInStock = quantity,
			UnitPrice = unitPrice,
			Category = categoryId,
			IsTrackable = IsTrackableCheckBox.Checked
		};

		// Optional Attributes
		if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts))
			command.Manufacturer = ManufacturerTextBox.Texts;

		if (!string.IsNullOrWhiteSpace(BrandTextBox.Texts))
			command.Brand = BrandTextBox.Texts;

		if (decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var groupPrice))
			command.GroupPrice = groupPrice;

		if (int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var groupPriceQuantity))
			command.GroupPriceQuantity = groupPriceQuantity;

		return command;
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