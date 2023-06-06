using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Sale;

[ExcludeFromCodeCoverage]
public partial class UpdateInvoiceProductForm : Form
{
	private readonly ISaleService _saleService;
	private readonly MessageForm _messageForm;
	private Product _product;

	public UpdateInvoiceProductForm(ISaleService saleService, MessageForm messageForm)
	{
		_saleService = saleService;
		_messageForm = messageForm;
		_product = new Product();

		InitializeComponent();
	}

	public void ShowDialog(string barcode, int priority)
	{
		try
		{
			_product = _saleService.GetSaleInvoiceProduct(barcode, priority);

			PopulateProductProperties(_product);

			ShowDialog();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"ไม่พบรหัสสินค้า {barcode} ลำดับที่ {priority} ในรายการขาย Error: {ex.Message}", "ไม่พบสินค้าในรายการขาย");
		}
	}

	private void PopulateProductProperties(Product product)
	{
		ProductCodeLabel.Text = product.Barcode;
		DescriptionLabel.Text = product.Description;
		QuantityTextBox.Texts = $"{product.Quantity}";

		UnitPriceTextBox.Texts = $"{product.UnitPrice:N}";
		UnitPriceLabel.Visible = !product.IsTrackable;
		UnitPriceTextBox.Visible = !product.IsTrackable;
		NoteTextBox.Texts = string.Empty;
		NoteLabel.Visible = !product.IsTrackable;
		NoteTextBox.Visible = !product.IsTrackable;
	}

	private bool ValidateUserInputs()
	{
		if (!int.TryParse(QuantityTextBox.Texts.Trim(), out _))
		{
			_messageForm.ShowDialog("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
			return false;
		}

		if (UnitPriceTextBox.Visible && !decimal.TryParse(UnitPriceTextBox.Texts.Trim(), out _))
		{
			_messageForm.ShowDialog("กรุณาใส่ราคาสินค้าให้ถูกต้อง", "ราคาสินค้าไม่ถูกต้อง");
			return false;
		}

		return true;
	}

	private async void UpdateProductButton_Click(object sender, EventArgs e)
	{
		if (!ValidateUserInputs())
			return;

		try
		{
			var quantity = int.Parse(QuantityTextBox.Texts.Trim());

			if (quantity == 0)
			{
				_saleService.RemoveProduct(_product);

				Hide();
			}
			
			await _saleService.UpdateProductQuantityAsync(_product.InventoryProductId, _product.Priority, quantity);

			if (!_product.IsTrackable)
			{
				var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
				var note = NoteTextBox.Texts.Trim();

				_saleService.UpdateProductUnitPrice(_product.InventoryProductId, _product.Priority, unitPrice, note);
			}
			
			Hide();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า Error: {ex.Message}", "เกิดความผิดพลาดในขณะที่กำลังอัพเดทสินค้า");
		}
	}

	private void CancelUpdateProductButton_Click(object sender, EventArgs e)
	{
		Hide();
	}

	private void RemoveProductButton_Click(object sender, EventArgs e)
	{
		_saleService.RemoveProduct(_product);
		
		Hide();
	}

	private void IncreaseQuantityButton_Click(object sender, EventArgs e)
	{
		if (!int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity)) 
			return;

		QuantityTextBox.Texts = $"{quantity + 1}";
	}

	private void DecreaseQuantityButton_Click(object sender, EventArgs e)
	{
		if (!int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity)) 
			return;

		QuantityTextBox.Texts = $"{quantity - 1}";
	}
}