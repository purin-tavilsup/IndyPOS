using IndyPOS.Application.Common.Interfaces;
using MediatR;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.InventoryProducts.Delete;
using IndyPOS.Application.UseCases.InventoryProducts.Update;

namespace IndyPOS.Windows.Forms.UI.Inventory;

[ExcludeFromCodeCoverage]
public partial class UpdateInventoryProductForm : Form
{
	private readonly IMediator _mediator;
	private readonly MessageForm _messageForm;
	private readonly IReadOnlyDictionary<int, string> _productCategoryDictionary;
	private InventoryProductDto? _product;

	public UpdateInventoryProductForm(IStoreConstants storeConstants,
									  IMediator mediator,
									  MessageForm messageForm)
	{
		_mediator = mediator;
		_productCategoryDictionary = storeConstants.ProductCategories;
		_messageForm = messageForm;
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
			var command = CreateCommandForUpdateProduct(_product);

			await _mediator.Send(command);

			Close();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า");
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
			GroupPrice = decimal.Parse(GroupPriceTextBox.Texts.Trim()),
			Category = categoryId
		};

		// Optional Attributes
		if (!string.IsNullOrWhiteSpace(ManufacturerTextBox.Texts))
			command.Manufacturer = ManufacturerTextBox.Texts;

		if (!string.IsNullOrWhiteSpace(BrandTextBox.Texts))
			command.Brand = BrandTextBox.Texts;

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
		if (_product is null)
		{
			return;
		}
		
		try
		{
			var command = new DeleteInventoryProductCommand(_product.InventoryProductId);

			await _mediator.Send(command);

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