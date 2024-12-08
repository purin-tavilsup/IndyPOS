using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.UseCases.InventoryProducts;

namespace IndyPOS.Windows.Forms.UI.Sale;

[ExcludeFromCodeCoverage]
public partial class AddInvoiceProductForm : Form
{
	private readonly ISaleService _saleService;
	private readonly MessageForm _messageForm;
	private InventoryProductDto? _product;

	public AddInvoiceProductForm(ISaleService saleService, MessageForm messageForm)
	{
		_saleService = saleService;
		_messageForm = messageForm;
		_product = null;

		InitializeComponent();
	}

	public async Task ShowDialog(string barcode)
	{
		try
		{
			_product = await _saleService.GetInventoryProductByBarcodeAsync(barcode);

			PopulateProductProperties(_product);

			ShowDialog();
		}
		catch (Exception ex)
		{
			_messageForm.ShowDialog($"ไม่พบรหัสสินค้า {barcode} ในระบบ Error: {ex.Message}", "ไม่พบสินค้าในระบบ");
		}
	}

	private void PopulateProductProperties(InventoryProductDto product)
	{
		ProductCodeLabel.Text = product.Barcode;
		DescriptionLabel.Text = product.Description;
		QuantityTextBox.Texts = "1";
		UnitPriceTextBox.Texts = string.Empty;
		NoteTextBox.Texts = string.Empty;
	}

	private bool ValidateQuantityEntry()
	{
		var isQuantityValid = int.TryParse(QuantityTextBox.Texts.Trim(), out var quantity);

		if (!isQuantityValid || quantity == 0)
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

	private void AddProductButton_Click(object sender, EventArgs e)
	{
		if (_product is null || !ValidateQuantityEntry())
			return;

		var quantity = int.Parse(QuantityTextBox.Texts.Trim());
		var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
		var note = NoteTextBox.Texts.Trim();

		_saleService.AddProduct(_product, unitPrice, quantity, note);
		
		Hide();
	}

	private void CancelUpdateProductButton_Click(object sender, EventArgs e)
	{
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