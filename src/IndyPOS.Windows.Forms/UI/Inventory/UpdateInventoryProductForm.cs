using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.InventoryProducts;
using IndyPOS.Application.InventoryProducts.Commands.DeleteInventoryProduct;
using IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProduct;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Inventory;

[ExcludeFromCodeCoverage]
public partial class UpdateInventoryProductForm : Form
{
	private readonly IMediator _mediator;
	private readonly MessageForm _messageForm;
	private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
	private InventoryProductDto _product = new();

	public UpdateInventoryProductForm(IStoreConstants storeConstants,
									  IMediator mediator,
									  MessageForm messageForm)
	{
		_mediator = mediator;
		_productCategoryDictionary = storeConstants.ProductCategories;
		_messageForm = messageForm;

		InitializeComponent();
		InitializeProductCategories();
	}

	public void ShowDialog(InventoryProductDto product)
	{
		_product = product;

		BarcodeTextBox.Texts = _product.Barcode;

		PopulateProductProperties();

		CancelUpdateProductButton.Select();

		RemoveProductButton.Enabled = product.IsTrackable;

		ShowDialog();
	}

	private void PopulateProductProperties()
	{
		DescriptionTextBox.Texts = _product.Description;
		QuantityLabel.Text = $"{_product.QuantityInStock}";
		UnitPriceTextBox.Texts = $"{_product.UnitPrice:N}";
		CategoryComboBox.Texts = _productCategoryDictionary[_product.Category];
		GroupPriceTextBox.Texts = _product.GroupPrice.HasValue ? $"{_product.GroupPrice.Value:N}" : string.Empty;
		GroupPriceQuantityTextBox.Texts = _product.GroupPriceQuantity.HasValue ? $"{_product.GroupPriceQuantity.Value}" : string.Empty;
		ManufacturerTextBox.Texts = _product.Manufacturer;
		BrandTextBox.Texts = _product.Brand;
	}

	private bool ValidateProductEntry()
	{
		if (string.IsNullOrWhiteSpace(DescriptionTextBox.Texts))
		{
			_messageForm.Show("กรุณาใส่คำอธิบายสินค้าให้ถูกต้อง", "คำอธิบายสินค้าไม่ถูกต้อง");
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

	private async void UpdateProductButton_Click(object sender, EventArgs e)
	{
		if (!ValidateProductEntry())
			return;

		try
		{
			var command = CreateCommandForUpdateProduct(_product);

			await _mediator.Send(command);

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า");
		}
	}

	private UpdateInventoryProductCommand CreateCommandForUpdateProduct(InventoryProductDto product)
	{
		var category = _productCategoryDictionary.FirstOrDefault(x => x.Value == CategoryComboBox.Texts);
		var categoryId = category.Key;

		var command = new UpdateInventoryProductCommand
		{
			Id = product.InventoryProductId,
			Description = DescriptionTextBox.Texts.Trim(),
			QuantityInStock = int.Parse(QuantityLabel.Text.Trim()),
			UnitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim()),
			Category = categoryId
		};

		// Optional Attributes
		if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts))
			command.Manufacturer = ManufacturerTextBox.Texts;

		if (!string.IsNullOrWhiteSpace(BrandTextBox.Texts))
			command.Brand = BrandTextBox.Texts;

		if (decimal.TryParse(GroupPriceTextBox.Texts.Trim(), out var groupPrice))
		{
			command.GroupPrice = groupPrice;
		}
		else
		{
			command.GroupPrice = null;
		}

		if (int.TryParse(GroupPriceQuantityTextBox.Texts.Trim(), out var groupPriceQuantity))
		{
			command.GroupPriceQuantity = groupPriceQuantity;
		}
		else
		{
			command.GroupPriceQuantity = null;
		}

		return command;
	}

	private void CancelUpdateProductButton_Click(object sender, EventArgs e)
	{
		Close();
	}

	private async void RemoveProductButton_Click(object sender, EventArgs e)
	{
		try
		{
			var command = new DeleteInventoryProductCommand(_product.InventoryProductId);

			await _mediator.Send(command);

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.Show($"เกิดความผิดพลาดในขณะที่กำลังลบสินค้า Error: {ex.Message}",
							  "เกิดความผิดพลาดในขณะที่กำลังลบสินค้า");
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
				_messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
				return false;
			}
		}
		else
		{
			_messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
			return false;
		}

		return true;
	}
}