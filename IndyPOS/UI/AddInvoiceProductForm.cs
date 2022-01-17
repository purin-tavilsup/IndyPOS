using IndyPOS.Controllers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.UI
{
    [ExcludeFromCodeCoverage]
	public partial class AddInvoiceProductForm : Form
    {
        private readonly ISaleInvoiceController _saleInvoiceController;
		private readonly MessageForm _messageForm;
        private InventoryProductModel _product;

        public AddInvoiceProductForm(ISaleInvoiceController saleInvoiceController,
									 MessageForm messageForm)
        {
            _saleInvoiceController = saleInvoiceController;
			_messageForm = messageForm;

            InitializeComponent();
        }

        public void ShowDialog(string barcode)
		{
			_product = _saleInvoiceController.GetInventoryProductByBarcode(barcode);

            PopulateProductProperties(_product);

            CancelUpdateProductButton.Select();

            ShowDialog();
        }

        private void PopulateProductProperties(InventoryProductModel product)
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
				_messageForm.Show("กรุณาใส่จำนวนสินค้าให้ถูกต้อง", "จำนวนสินค้าไม่ถูกต้อง");
				return false;
			}

            if (UnitPriceTextBox.Visible && !decimal.TryParse(UnitPriceTextBox.Texts.Trim(), out _))
            {
				_messageForm.Show("กรุณาใส่ราคาสินค้าให้ถูกต้อง", "ราคาสินค้าไม่ถูกต้อง");
				return false;
            }

            return true;
        }

        private void AddProductButton_Click(object sender, EventArgs e)
        {
            if (!ValidateQuantityEntry())
                return;

            var quantity = int.Parse(QuantityTextBox.Texts.Trim());
			var unitPrice = decimal.Parse(UnitPriceTextBox.Texts.Trim());
			var note = NoteTextBox.Texts.Trim();

			_saleInvoiceController.AddProduct(_product, unitPrice, quantity, note);
            
            Close();
        }

        private void CancelUpdateProductButton_Click(object sender, EventArgs e)
        {
            Close();
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
}
